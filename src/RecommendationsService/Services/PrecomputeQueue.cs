using System.Threading.Channels;
using RecommendationsService.Models;

namespace RecommendationsService.Services;

public class PrecomputeQueue : IPrecomputeQueue
{
    private readonly Channel<PrecomputeRequest> _channel;
    private readonly ILogger<PrecomputeQueue> _logger;

    public PrecomputeQueue(ILogger<PrecomputeQueue> logger)
    {
        _logger = logger;
        // Bounded channel to prevent unbounded memory growth under extreme load
        var options = new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<PrecomputeRequest>(options);
    }

    public async ValueTask QueuePrecomputeAsync(PrecomputeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Queueing out-of-band precompute job for User '{UserId}' (Priority: {Priority})",
            request.UserId, request.Priority);

        await _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<PrecomputeRequest> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
