namespace PolyglotPersistenceDemo.Models.Cosmos;

public record VideoTelemetry(
    string Id,
    string UserId,
    string DeviceId,
    double AvgBitrateMbps,
    double BufferRatio,
    string CurrentResolution,
    DateTimeOffset RecordedAt
);
