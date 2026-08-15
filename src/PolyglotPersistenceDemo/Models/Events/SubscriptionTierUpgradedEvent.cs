namespace PolyglotPersistenceDemo.Models.Events;

public record SubscriptionTierUpgradedEvent(
    string EventId,
    string UserId,
    string OldTier,
    string NewTier,
    int NewMaxStreams,
    string NewMaxResolution,
    DateTimeOffset Timestamp
);
