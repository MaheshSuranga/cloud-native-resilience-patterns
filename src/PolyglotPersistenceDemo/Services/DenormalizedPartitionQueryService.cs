using System.Diagnostics;
using PolyglotPersistenceDemo.Models.Cosmos;
using PolyglotPersistenceDemo.Stores;

namespace PolyglotPersistenceDemo.Services;

public class DenormalizedPartitionQueryService
{
    private readonly ICosmosTelemetryStore _cosmosStore;

    public DenormalizedPartitionQueryService(ICosmosTelemetryStore cosmosStore)
    {
        _cosmosStore = cosmosStore;
    }

    public async Task<(List<DenormalizedUserProfile> Results, long ElapsedMilliseconds, int TotalQueries)> ExecuteSinglePartitionQueriesAsync(IEnumerable<string> userIds)
    {
        var sw = Stopwatch.StartNew();
        var idList = userIds.ToList();

        // High-throughput parallel single-partition point reads (O(1) complexity per user)
        var tasks = idList.Select(userId => _cosmosStore.GetDenormalizedUserProfileAsync(userId));
        var profiles = await Task.WhenAll(tasks);

        sw.Stop();
        var validProfiles = profiles.Where(p => p != null).Select(p => p!).ToList();
        return (validProfiles, sw.ElapsedMilliseconds, idList.Count);
    }
}
