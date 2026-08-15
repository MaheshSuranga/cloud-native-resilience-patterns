using PolyglotPersistenceDemo.Models.Sql;

namespace PolyglotPersistenceDemo.Stores;

public interface ISqlBillingStore
{
    Task<BillingAccount?> GetBillingAccountAsync(string userId);
    Task<IReadOnlyList<BillingAccount>> GetAllActiveBillingAccountsAsync();
    Task<SubscriptionPlan?> GetSubscriptionPlanAsync(string planId);
    Task UpdateSubscriptionPlanAsync(string userId, string newPlanId);
}
