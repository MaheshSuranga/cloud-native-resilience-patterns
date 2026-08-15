using RecommendationsService.Models;

namespace RecommendationsService.Services;

public interface IHomepagePrecomputeEngine
{
    HomepageLayoutResponse GetGlobalDefaultLayout();
    Task<HomepageLayoutResponse> ComputeAndCacheLayoutAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> BatchPrecomputeAndCacheAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}
