using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
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
builder.Services.AddSwaggerGen();

// Register Recommendations & EVCache Precompute Services
builder.Services.AddSingleton<IRecommendationsEngine, RecommendationsEngine>();
builder.Services.AddSingleton<IPrecomputeQueue, PrecomputeQueue>();
builder.Services.AddSingleton<IHomepagePrecomputeEngine, HomepagePrecomputeEngine>();
builder.Services.AddHostedService<EVCachePrecomputeWorker>();

// Register Distributed Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "rec:";
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

    // Strategy 1: Hard Timeout (2.0 seconds)
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
    IDistributedCache cache,
    IEntitlementsClient entitlementsClient,
    IRecommendationsEngine recommendationsEngine,
    ILogger<Program> logger,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var cacheKey = $"user:{userId}";

    // Phase 1: Cache-Aside Check
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

    logger.LogInformation("Cache MISS for key '{CacheKey}'. Calling downstream EntitlementsService.", cacheKey);
    httpContext.Response.Headers.Append("X-Cache", "MISS");

    // Phase 2: Downstream Call via Polly v8 Resilience Pipeline
    UserEntitlementDto entitlement;
    try
    {
        entitlement = await entitlementsClient.GetEntitlementsAsync(userId, cancellationToken);
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
                Reason: "CircuitBreakerOpen",
                RetryAfterSeconds: 15,
                Message: "Downstream entitlements service timed out. Resilience pipeline triggered fallback."
            ),
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Downstream call failed for User '{UserId}'. Returning 503 degraded fallback.", userId);
        httpContext.Response.Headers.Append("Retry-After", "15");
        return Results.Json(
            new DegradedResponse(
                Status: "Degraded",
                Reason: "CircuitBreakerOpen",
                RetryAfterSeconds: 15,
                Message: "Downstream service invocation failed."
            ),
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }

    // Phase 3: Compute Recommendations
    var recommendations = recommendationsEngine.GenerateRecommendations(entitlement);

    // Phase 4: Write to Cache-Aside with 60-second sliding expiration
    try
    {
        var cacheOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(60),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        var serialized = JsonSerializer.Serialize(recommendations);
        await cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
        logger.LogInformation("Successfully cached recommendations for key '{CacheKey}' with 60s sliding TTL.", cacheKey);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Redis cache write failed for key '{CacheKey}'. Continuing response flow.", cacheKey);
    }

    return Results.Ok(recommendations);
})
.WithName("GetRecommendations")
.WithOpenApi();

// -----------------------------------------------------------------------------
// STEP 4 PATTERN: Netflix-Style EVCache (Primary Store Zero-SQL Read API)
// -----------------------------------------------------------------------------
app.MapGet("/homepage/{userId}", async (
    string userId,
    IDistributedCache cache,
    IHomepagePrecomputeEngine precomputeEngine,
    IPrecomputeQueue precomputeQueue,
    ILogger<Program> logger,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var cacheKey = $"user:{userId}:homepage:v1";

    try
    {
        // 1. Direct Zero-SQL Read from Redis Primary Store
        var cachedHomepage = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedHomepage))
        {
            logger.LogInformation("[EVCache Read] Primary Store HIT for '{CacheKey}'", cacheKey);
            httpContext.Response.Headers.Append("X-Cache-Store", "EVCache-Primary");
            httpContext.Response.Headers.Append("X-Source", "Precomputed");

            var layout = JsonSerializer.Deserialize<HomepageLayoutResponse>(cachedHomepage);
            if (layout is not null)
            {
                return Results.Ok(layout);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "[EVCache Read] Redis primary store lookup failed for '{CacheKey}'", cacheKey);
    }

    // 2. Fail-Fast Cold Cache Miss:
    // Do NOT execute synchronized heavy SQL or downstream calls on the request thread!
    logger.LogInformation("[EVCache Read] Cache MISS for '{CacheKey}'. Dispatching out-of-band precompute job.", cacheKey);
    httpContext.Response.Headers.Append("X-Cache-Store", "EVCache-Miss-Fallback");
    httpContext.Response.Headers.Append("X-Source", "GlobalDefault");

    // Enqueue out-of-band background calculation without blocking
    await precomputeQueue.QueuePrecomputeAsync(
        new PrecomputeRequest(UserId: userId, Priority: "High", QueuedAt: DateTimeOffset.UtcNow),
        CancellationToken.None
    );

    // Immediately return the global default fallback layout
    var defaultLayout = precomputeEngine.GetGlobalDefaultLayout();
    return Results.Ok(defaultLayout);
})
.WithName("GetHomepageLayout")
.WithOpenApi();

// -----------------------------------------------------------------------------
// STEP 4 BATCH TRIGGER: Parallel Batch Pre-computation Engine
// -----------------------------------------------------------------------------
app.MapPost("/homepage/precompute/batch", async (
    string[]? userIds,
    IHomepagePrecomputeEngine precomputeEngine,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var targetUsers = userIds != null && userIds.Length > 0
        ? userIds
        : new[] { "user1", "user2", "user3", "user4", "user5", "user123", "user4k", "vip_customer" };

    var sw = Stopwatch.StartNew();
    var count = await precomputeEngine.BatchPrecomputeAndCacheAsync(targetUsers, cancellationToken);
    sw.Stop();

    return Results.Ok(new
    {
        status = "Completed",
        processedUsers = count,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        timestamp = DateTimeOffset.UtcNow
    });
})
.WithName("TriggerBatchPrecompute")
.WithOpenApi();

app.Run();
