using RecommendationsService.Models;

namespace RecommendationsService.Services;

public interface IPrecomputeQueue
{
    ValueTask QueuePrecomputeAsync(PrecomputeRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PrecomputeRequest> ReadAllAsync(CancellationToken cancellationToken = default);
}
