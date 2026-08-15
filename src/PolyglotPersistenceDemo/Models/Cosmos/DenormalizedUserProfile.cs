namespace PolyglotPersistenceDemo.Models.Cosmos;

public record DenormalizedUserProfile(
    string Id,
    string UserId,
    string Tier,
    int MaxStreams,
    string MaxResolution,
    string AccountStatus,
    IReadOnlyList<ViewingHistory> RecentHistory,
    VideoTelemetry? ActiveTelemetry,
    DateTimeOffset LastSynchronizedAt
);
