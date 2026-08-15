namespace PolyglotPersistenceDemo.Models.Cosmos;

public record ViewingHistory(
    string Id,
    string UserId,
    string VideoId,
    string Title,
    int ProgressSeconds,
    bool Completed,
    DateTimeOffset LastWatchedAt
);
