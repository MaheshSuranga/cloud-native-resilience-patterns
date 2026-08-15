using EntitlementsService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "EntitlementsService",
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/entitlements/{userId}", async (
    string userId,
    int? simulateDelay,
    bool? simulateError,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Processing entitlement lookup for User '{UserId}'. SimulateDelay={Delay}ms, SimulateError={SimulateError}",
        userId, simulateDelay ?? 0, simulateError ?? false);

    if (simulateDelay.HasValue && simulateDelay.Value > 0)
    {
        logger.LogWarning("Injecting simulated delay of {Delay}ms for User '{UserId}'", simulateDelay.Value, userId);
        await Task.Delay(simulateDelay.Value, cancellationToken);
    }

    if (simulateError.HasValue && simulateError.Value)
    {
        logger.LogError("Injecting simulated failure (HTTP 500) for User '{UserId}'", userId);
        return Results.Problem(
            detail: $"Simulated downstream failure in EntitlementsService for User '{userId}'",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "DownstreamServiceError"
        );
    }

    // Determine tier deterministically based on user ID
    var isPremium = userId.Contains("premium", StringComparison.OrdinalIgnoreCase) ||
                    userId.Contains("4k", StringComparison.OrdinalIgnoreCase) ||
                    (int.TryParse(userId.Replace("user", "", StringComparison.OrdinalIgnoreCase), out var idNum) && idNum % 2 == 0) ||
                    userId.Equals("user123", StringComparison.OrdinalIgnoreCase);

    var tier = isPremium ? "4K" : "Standard";
    var activeFeatures = isPremium
        ? new[] { "UltraHD", "DolbyAtmos", "SpatialAudio", "OfflineDownloads", "MultiScreen4Stream" }
        : new[] { "FullHD", "StereoAudio", "SingleScreen" };

    var entitlement = new UserEntitlement(
        UserId: userId,
        IsPremium: isPremium,
        Tier: tier,
        ActiveFeatures: activeFeatures
    );

    return Results.Ok(entitlement);
})
.WithName("GetUserEntitlement")
.WithOpenApi();

app.Run();
