namespace PolyglotPersistenceDemo.Models.Sql;

public record SubscriptionPlan(
    string PlanId,
    string TierName,
    decimal PricePerMonth,
    int MaxConcurrentStreams,
    string MaxResolution
);
