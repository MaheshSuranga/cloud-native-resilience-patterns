using System.Diagnostics;
using PolyglotPersistenceDemo.Models.Cosmos;
using PolyglotPersistenceDemo.Stores;

namespace PolyglotPersistenceDemo.Services;

public class AntiPatternDistributedJoinService
{
    private readonly ISqlBillingStore _sqlStore;
    private readonly ICosmosTelemetryStore _cosmosStore;

    public AntiPatternDistributedJoinService(ISqlBillingStore sqlStore, ICosmosTelemetryStore cosmosStore)
    {
        _sqlStore = sqlStore;
        _cosmosStore = cosmosStore;
    }

    public async Task<(List<DenormalizedUserProfile> Results, long ElapsedMilliseconds, int TotalQueries)> ExecuteDistributedJoinAsync(int userCount)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<DenormalizedUserProfile>();

        // 1. Initial Relational SQL query to get active accounts
        var accounts = (await _sqlStore.GetAllActiveBillingAccountsAsync()).Take(userCount).ToList();

        // 2. Loop N Users: Cross-boundary Distributed Join (O(N) network hops)
        foreach (var account in accounts)
        {
            // Hop A: Query SQL Plan for each account
            var plan = await _sqlStore.GetSubscriptionPlanAsync(account.PlanId);

            // Hop B: Query Cosmos DB for Viewing History (Partition: /userId)
            var history = await _cosmosStore.GetViewingHistoryByUserIdAsync(account.UserId);

            // Hop C: Query Cosmos DB for Active Telemetry (Partition: /userId)
            var telemetry = await _cosmosStore.GetActiveTelemetryByUserIdAsync(account.UserId);

            // Hop D: In-Memory Application Join
            var profile = new DenormalizedUserProfile(
                Id: $"profile_{account.UserId}",
                UserId: account.UserId,
                Tier: plan?.TierName ?? "Unknown",
                MaxStreams: plan?.MaxConcurrentStreams ?? 1,
                MaxResolution: plan?.MaxResolution ?? "720p",
                AccountStatus: account.Status,
                RecentHistory: history,
                ActiveTelemetry: telemetry,
                LastSynchronizedAt: DateTimeOffset.UtcNow
            );

            results.Add(profile);
        }

        sw.Stop();
        // Total queries = 1 (initial accounts) + N * 3 (plan + history + telemetry) = 3N + 1 queries!
        var totalQueries = 1 + (accounts.Count * 3);
        return (results, sw.ElapsedMilliseconds, totalQueries);
    }
}
