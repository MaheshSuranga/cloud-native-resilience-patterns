using PolyglotPersistenceDemo.Models.Events;
using PolyglotPersistenceDemo.Stores;

namespace PolyglotPersistenceDemo.Services;

public class EventDrivenDenormalizer
{
    private readonly ISqlBillingStore _sqlStore;
    private readonly ICosmosTelemetryStore _cosmosStore;

    public EventDrivenDenormalizer(ISqlBillingStore sqlStore, ICosmosTelemetryStore cosmosStore)
    {
        _sqlStore = sqlStore;
        _cosmosStore = cosmosStore;
    }

    public async Task<SubscriptionTierUpgradedEvent> UpgradeSubscriptionAsync(string userId, string newPlanId)
    {
        // 1. Relational ACID Transaction on Azure SQL Billing Store
        await _sqlStore.UpdateSubscriptionPlanAsync(userId, newPlanId);
        var plan = await _sqlStore.GetSubscriptionPlanAsync(newPlanId);

        // 2. Formulate Integration Event
        var @event = new SubscriptionTierUpgradedEvent(
            EventId: Guid.NewGuid().ToString("N"),
            UserId: userId,
            OldTier: "Standard HD",
            NewTier: plan?.TierName ?? "4K Ultra HDR",
            NewMaxStreams: plan?.MaxConcurrentStreams ?? 4,
            NewMaxResolution: plan?.MaxResolution ?? "4K UHD",
            Timestamp: DateTimeOffset.UtcNow
        );

        // 3. Asynchronous Event Handler: Update Denormalized Profile in Cosmos DB
        await HandleSubscriptionTierUpgradedAsync(@event);

        return @event;
    }

    public async Task HandleSubscriptionTierUpgradedAsync(SubscriptionTierUpgradedEvent @event)
    {
        // Fetch existing profile document within the user's partition
        var currentProfile = await _cosmosStore.GetDenormalizedUserProfileAsync(@event.UserId);
        if (currentProfile != null)
        {
            var updatedProfile = currentProfile with
            {
                Tier = @event.NewTier,
                MaxStreams = @event.NewMaxStreams,
                MaxResolution = @event.NewMaxResolution,
                LastSynchronizedAt = DateTimeOffset.UtcNow
            };

            await _cosmosStore.UpsertDenormalizedUserProfileAsync(updatedProfile);
        }
    }
}
