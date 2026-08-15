using RecommendationsService.Services;

namespace RecommendationsService.BackgroundServices;

public class EVCachePrecomputeWorker : BackgroundService
{
    private readonly IPrecomputeQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EVCachePrecomputeWorker> _logger;

    public EVCachePrecomputeWorker(
        IPrecomputeQueue queue,
        IServiceProvider serviceProvider,
        ILogger<EVCachePrecomputeWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EVCache Precompute Worker started. Initializing background channel listener and periodic batch runner...");

        // Task 1: Out-of-band Channel Consumer Loop
        var channelConsumerTask = ProcessChannelRequestsAsync(stoppingToken);

        // Task 2: Periodic Batch Precompute Engine (Simulating active user cohort background calculations)
        var periodicBatchTask = RunPeriodicBatchPrecomputeAsync(stoppingToken);

        await Task.WhenAll(channelConsumerTask, periodicBatchTask);
    }

    private async Task ProcessChannelRequestsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var request in _queue.ReadAllAsync(stoppingToken))
            {
                _logger.LogInformation("[EVCache Channel Consumer] Picked up out-of-band job for User '{UserId}' (QueuedAt: {QueuedAt})",
                    request.UserId, request.QueuedAt);

                using var scope = _serviceProvider.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IHomepagePrecomputeEngine>();

                try
                {
                    await engine.ComputeAndCacheLayoutAsync(request.UserId, stoppingToken);
                    _logger.LogInformation("[EVCache Channel Consumer] Successfully precomputed and persisted layout for User '{UserId}'", request.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EVCache Channel Consumer] Error processing precompute job for User '{UserId}'", request.UserId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[EVCache Channel Consumer] Shutting down gracefully.");
        }
    }

    private async Task RunPeriodicBatchPrecomputeAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        var seedUsers = new[] { "user123", "user4k", "user_std", "premium_user_1", "vip_customer" };

        try
        {
            // Initial seed precompute on startup
            using (var scope = _serviceProvider.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<IHomepagePrecomputeEngine>();
                await engine.BatchPrecomputeAndCacheAsync(seedUsers, stoppingToken);
            }

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("[EVCache Periodic Batch] Running scheduled cohort precomputation...");
                using var scope = _serviceProvider.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IHomepagePrecomputeEngine>();
                await engine.BatchPrecomputeAndCacheAsync(seedUsers, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[EVCache Periodic Batch] Shutting down gracefully.");
        }
    }
}
