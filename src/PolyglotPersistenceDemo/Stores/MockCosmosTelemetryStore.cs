using System.Collections.Concurrent;
using PolyglotPersistenceDemo.Models.Cosmos;

namespace PolyglotPersistenceDemo.Stores;

public class MockCosmosTelemetryStore : ICosmosTelemetryStore
{
    private readonly ConcurrentDictionary<string, List<ViewingHistory>> _history = new();
    private readonly ConcurrentDictionary<string, VideoTelemetry> _telemetry = new();
    private readonly ConcurrentDictionary<string, DenormalizedUserProfile> _profiles = new();

    private int _pointReadCount = 0;
    private double _consumedRUs = 0.0;

    public int TotalPointReads => _pointReadCount;
    public double TotalConsumedRUs => _consumedRUs;

    public MockCosmosTelemetryStore()
    {
        for (int i = 1; i <= 50; i++)
        {
            var userId = $"user_{i:D3}";
            _history[userId] = new List<ViewingHistory>
            {
                new($"hist_{i}_1", userId, "vid_01", "Interstellar Odyssey", 1420, false, DateTimeOffset.UtcNow.AddHours(-2)),
                new($"hist_{i}_2", userId, "vid_02", "Cyberpunk 2099", 5400, true, DateTimeOffset.UtcNow.AddDays(-1))
            };

            _telemetry[userId] = new VideoTelemetry(
                $"tel_{i}", userId, $"dev_tv_{i}", 15.4, 0.001, i % 2 == 0 ? "4K UHD" : "1080p", DateTimeOffset.UtcNow
            );

            // Initial denormalized profile
            var tier = i % 2 == 0 ? "4K Ultra HDR" : "Standard HD";
            var maxStreams = i % 2 == 0 ? 4 : 2;
            var maxRes = i % 2 == 0 ? "4K UHD" : "1080p";

            _profiles[userId] = new DenormalizedUserProfile(
                Id: $"profile_{userId}",
                UserId: userId,
                Tier: tier,
                MaxStreams: maxStreams,
                MaxResolution: maxRes,
                AccountStatus: "Active",
                RecentHistory: _history[userId],
                ActiveTelemetry: _telemetry[userId],
                LastSynchronizedAt: DateTimeOffset.UtcNow
            );
        }
    }

    public async Task<IReadOnlyList<ViewingHistory>> GetViewingHistoryByUserIdAsync(string userId)
    {
        Interlocked.Increment(ref _pointReadCount);
        _consumedRUs += 2.5;
        await Task.Delay(4); // Single partition query latency
        _history.TryGetValue(userId, out var list);
        return list ?? new List<ViewingHistory>();
    }

    public async Task<VideoTelemetry?> GetActiveTelemetryByUserIdAsync(string userId)
    {
        Interlocked.Increment(ref _pointReadCount);
        _consumedRUs += 1.0;
        await Task.Delay(3); // Point read
        _telemetry.TryGetValue(userId, out var tel);
        return tel;
    }

    public async Task<DenormalizedUserProfile?> GetDenormalizedUserProfileAsync(string userId)
    {
        Interlocked.Increment(ref _pointReadCount);
        _consumedRUs += 1.0; // Point read by ID and PartitionKey /userId = 1.0 RU!
        await Task.Delay(3); // Fast Cosmos DB point read (< 5ms)
        _profiles.TryGetValue(userId, out var profile);
        return profile;
    }

    public async Task UpsertDenormalizedUserProfileAsync(DenormalizedUserProfile profile)
    {
        Interlocked.Increment(ref _pointReadCount);
        _consumedRUs += 5.5; // Upsert RU cost
        await Task.Delay(6);
        _profiles[profile.UserId] = profile;
    }

    public void ResetMetrics()
    {
        Interlocked.Exchange(ref _pointReadCount, 0);
        _consumedRUs = 0.0;
    }
}
