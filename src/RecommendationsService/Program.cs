using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RecommendationsService.BackgroundServices;
using RecommendationsService.Clients;
using RecommendationsService.Models;
using RecommendationsService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Recommendations & EVCache Microservice API", Version = "v1" });
});

// Register Recommendations & EVCache Precompute Services
builder.Services.AddSingleton<IRecommendationsEngine, RecommendationsEngine>();
builder.Services.AddSingleton<IPrecomputeQueue, PrecomputeQueue>();
builder.Services.AddSingleton<IHomepagePrecomputeEngine, HomepagePrecomputeEngine>();
builder.Services.AddHostedService<EVCachePrecomputeWorker>();

// Register Memory Cache & Distributed Redis Cache with Resilient Hybrid Fallback
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IDistributedCache>(sp =>
{
    var memoryCache = sp.GetRequiredService<IMemoryCache>();
    var logger = sp.GetRequiredService<ILogger<ResilientDistributedCache>>();
    var config = sp.GetRequiredService<IConfiguration>();

    var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
    if (!redisConn.Contains("connectTimeout", StringComparison.OrdinalIgnoreCase))
    {
        redisConn += ",connectTimeout=300,syncTimeout=300,abortConnect=false";
    }

    var redisOptions = new RedisCacheOptions
    {
        Configuration = redisConn,
        InstanceName = "rec:"
    };

    var innerRedisCache = new RedisCache(redisOptions);
    return new ResilientDistributedCache(innerRedisCache, memoryCache, logger);
});

// Configure Resilient Entitlements HTTP Client with Polly v8 Resilience Pipeline
builder.Services.AddHttpClient<IEntitlementsClient, EntitlementsClient>(client =>
{
    var entitlementsUrl = builder.Configuration["Services:EntitlementsUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(entitlementsUrl);
})
.AddResilienceHandler("entitlements-resilience-pipeline", (pipelineBuilder, context) =>
{
    var serviceProvider = context.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Polly.ResiliencePipeline");

    // Strategy 1: Hard Timeout (2.0 seconds cancellation)
    pipelineBuilder.AddTimeout(new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(2)
    });

    // Strategy 2: Circuit Breaker (50% failure rate over 10s, min 4 requests, 15s break duration)
    pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 4,
        BreakDuration = TimeSpan.FromSeconds(15),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(response => (int)response.StatusCode >= 500),
        OnOpened = args =>
        {
            logger.LogWarning("[Polly Circuit Breaker] State -> OPEN. Downstream failure detected. Breaking circuit for {Duration}s.", args.BreakDuration.TotalSeconds);
            return ValueTask.CompletedTask;
        },
        OnClosed = args =>
        {
            logger.LogInformation("[Polly Circuit Breaker] State -> CLOSED. Normal downstream operations restored.");
            return ValueTask.CompletedTask;
        },
        OnHalfOpened = args =>
        {
            logger.LogInformation("[Polly Circuit Breaker] State -> HALF-OPEN. Probing downstream service with trial request.");
            return ValueTask.CompletedTask;
        }
    });
});

var app = builder.Build();

// Enable Swagger UI across all environments for developer experience
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recommendations API v1");
    c.RoutePrefix = "swagger";
});

// Root redirect to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));

// -----------------------------------------------------------------------------
// Health Check Endpoint
// -----------------------------------------------------------------------------
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "RecommendationsService",
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

// -----------------------------------------------------------------------------
// STEP 1 PATTERN: On-Demand Cache-Aside + Downstream Polly Resilience
// -----------------------------------------------------------------------------
app.MapGet("/recommendations/{userId}", async (
    string userId,
    int? simulateDelay,
    bool? simulateError,
    IDistributedCache cache,
    IEntitlementsClient entitlementsClient,
    IRecommendationsEngine recommendationsEngine,
    ILogger<Program> logger,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var isChaosRequest = (simulateDelay.HasValue && simulateDelay.Value > 0) || (simulateError.HasValue && simulateError.Value);
    var cacheKey = $"user:{userId}";

    // Phase 1: Cache-Aside Check (skip on explicit chaos requests so failure pipeline is tested)
    if (!isChaosRequest)
    {
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                logger.LogInformation("Cache HIT for key '{CacheKey}'", cacheKey);
                httpContext.Response.Headers.Append("X-Cache", "HIT");
                var cachedResponse = JsonSerializer.Deserialize<RecommendationsResponse>(cachedData);
                if (cachedResponse is not null)
                {
                    return Results.Ok(cachedResponse);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis cache read failed for key '{CacheKey}'. Falling through to downstream service.", cacheKey);
        }
    }

    logger.LogInformation("Cache MISS for key '{CacheKey}'. Calling downstream EntitlementsService.", cacheKey);
    httpContext.Response.Headers.Append("X-Cache", "MISS");

    // Phase 2: Downstream Call via Polly v8 Resilience Pipeline
    UserEntitlementDto entitlement;
    try
    {
        entitlement = await entitlementsClient.GetEntitlementsAsync(userId, simulateDelay, simulateError, cancellationToken);
    }
    catch (BrokenCircuitException ex)
    {
        logger.LogWarning(ex, "Circuit breaker is OPEN. Returning fast 503 degraded fallback.");
        httpContext.Response.Headers.Append("Retry-After", "15");
        return Results.Json(
            new DegradedResponse(
                Status: "Degraded",
                Reason: "CircuitBreakerOpen",
                RetryAfterSeconds: 15,
                Message: "Downstream entitlements service is temporarily unavailable. Circuit breaker is OPEN."
            ),
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
    catch (TimeoutRejectedException ex)
    {
        logger.LogWarning(ex, "Request timed out (>2s) in resilience pipeline. Returning 503 degraded fallback.");
        httpContext.Response.Headers.Append("Retry-After", "15");
        return Results.Json(
            new DegradedResponse(
                Status: "Degraded",
                Reason: "DownstreamTimeout",
                RetryAfterSeconds: 15,
                Message: "Downstream entitlements service timed out after 2.0s. Returning degraded fallback."
            ),
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
    catch (HttpRequestException ex)
    {
        logger.LogWarning(ex, "Downstream HTTP failure ({StatusCode}). Returning 503 fallback.", ex.StatusCode);
        httpContext.Response.Headers.Append("Retry-After", "15");
        return Results.Json(
            new DegradedResponse(
                Status: "Degraded",
                Reason: "DownstreamHttpError",
                RetryAfterSeconds: 15,
                Message: $"Downstream service failed with HTTP {ex.StatusCode}. Returning degraded fallback."
            ),
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }

    // Phase 3: Compute tier-aware recommendations
    var response = recommendationsEngine.GenerateRecommendations(entitlement);

    // Phase 4: Write to Cache-Aside with 60-second sliding expiration
    if (!isChaosRequest)
    {
        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(60),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            var serialized = JsonSerializer.Serialize(response);
            await cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
            logger.LogInformation("Written recommendations to cache key '{CacheKey}' (Sliding: 60s)", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for key '{CacheKey}'. Continuing response flow.", cacheKey);
        }
    }

    return Results.Ok(response);
})
.WithName("GetRecommendations")
.WithOpenApi();

// -----------------------------------------------------------------------------
// STEP 4 PATTERN: Netflix-Style EVCache Fast Zero-SQL Read API
// -----------------------------------------------------------------------------
app.MapGet("/homepage/{userId}", async (
    string userId,
    IDistributedCache cache,
    IPrecomputeQueue precomputeQueue,
    IHomepagePrecomputeEngine precomputeEngine,
    ILogger<Program> logger,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var sw = Stopwatch.StartNew();
    var cacheKey = $"user:{userId}:homepage:v1";

    // Fast Cache Read (< 5ms)
    try
    {
        var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            sw.Stop();
            logger.LogInformation("EVCache HIT for '{CacheKey}' in {ElapsedMs}ms", cacheKey, sw.ElapsedMilliseconds);
            httpContext.Response.Headers.Append("X-Cache-Store", "EVCache-Primary");
            httpContext.Response.Headers.Append("X-Read-Latency-Ms", sw.ElapsedMilliseconds.ToString());

            var layout = JsonSerializer.Deserialize<HomepageLayoutResponse>(cachedJson);
            if (layout is not null)
            {
                return Results.Ok(layout);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "EVCache read failed for '{CacheKey}'. Falling back to default layout.", cacheKey);
    }

    sw.Stop();
    logger.LogInformation("EVCache MISS for '{CacheKey}'. Fail-fast returning global fallback layout.", cacheKey);

    // Fail-Fast: Immediately return fast global static fallback without blocking on DB
    httpContext.Response.Headers.Append("X-Cache-Store", "EVCache-Miss-Fallback");
    httpContext.Response.Headers.Append("X-Read-Latency-Ms", sw.ElapsedMilliseconds.ToString());

    // Out-of-Band: Enqueue background job to compute and populate EVCache for next read
    await precomputeQueue.QueuePrecomputeAsync(new PrecomputeRequest(userId, "High", DateTimeOffset.UtcNow), cancellationToken);
    logger.LogInformation("Successfully enqueued out-of-band precompute job for user '{UserId}'", userId);

    var fallbackLayout = precomputeEngine.GetGlobalDefaultLayout();
    return Results.Ok(fallbackLayout);
})
.WithName("GetHomepageLayout")
.WithOpenApi();

// -----------------------------------------------------------------------------
// High-Throughput Batch Precompute Trigger Endpoint
// -----------------------------------------------------------------------------
app.MapPost("/homepage/precompute/batch", async (
    IHomepagePrecomputeEngine precomputeEngine,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var cohort = new[]
    {
        "user123",
        "user_std",
        "user_premium",
        "cold_user_99",
        "user_001",
        "user_002",
        "user_003",
        "user_004"
    };

    var sw = Stopwatch.StartNew();
    var count = await precomputeEngine.BatchPrecomputeAndCacheAsync(cohort, cancellationToken);
    sw.Stop();

    logger.LogInformation("Batch precomputed {Count} user homepages in {ElapsedMs}ms", count, sw.ElapsedMilliseconds);

    return Results.Ok(new
    {
        status = "Completed",
        processedUsers = count,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        timestamp = DateTimeOffset.UtcNow
    });
})
.WithName("BatchPrecompute")
.WithOpenApi();

app.Run();
