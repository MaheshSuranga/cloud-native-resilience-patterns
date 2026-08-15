using PolyglotPersistenceDemo.Models.Cosmos;

namespace PolyglotPersistenceDemo.Stores;

public interface ICosmosTelemetryStore
{
    Task<IReadOnlyList<ViewingHistory>> GetViewingHistoryByUserIdAsync(string userId);
    Task<VideoTelemetry?> GetActiveTelemetryByUserIdAsync(string userId);
    Task<DenormalizedUserProfile?> GetDenormalizedUserProfileAsync(string userId);
    Task UpsertDenormalizedUserProfileAsync(DenormalizedUserProfile profile);
}
