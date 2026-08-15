using System.Collections.Concurrent;
using PolyglotPersistenceDemo.Models.Sql;

namespace PolyglotPersistenceDemo.Stores;

public class MockSqlBillingStore : ISqlBillingStore
{
    private readonly ConcurrentDictionary<string, BillingAccount> _accounts = new();
    private readonly ConcurrentDictionary<string, SubscriptionPlan> _plans = new();
    private int _queryCount = 0;

    public int TotalQueriesExecuted => _queryCount;

    public MockSqlBillingStore()
    {
        // Seed Plans
        _plans["plan-standard"] = new SubscriptionPlan("plan-standard", "Standard HD", 12.99m, 2, "1080p");
        _plans["plan-premium-4k"] = new SubscriptionPlan("plan-premium-4k", "4K Ultra HDR", 19.99m, 4, "4K UHD");

        // Seed Accounts
        for (int i = 1; i <= 50; i++)
        {
            var userId = $"user_{i:D3}";
            var planId = i % 2 == 0 ? "plan-premium-4k" : "plan-standard";
            var rate = i % 2 == 0 ? 19.99m : 12.99m;
            _accounts[userId] = new BillingAccount($"acct_{i:D3}", userId, planId, "Active", rate, DateTimeOffset.UtcNow.AddDays(30));
        }
    }

    public async Task<BillingAccount?> GetBillingAccountAsync(string userId)
    {
        Interlocked.Increment(ref _queryCount);
        // Simulate relational database query network trip & index seek
        await Task.Delay(10);
        _accounts.TryGetValue(userId, out var account);
        return account;
    }

    public async Task<IReadOnlyList<BillingAccount>> GetAllActiveBillingAccountsAsync()
    {
        Interlocked.Increment(ref _queryCount);
        // Simulate bulk SELECT query
        await Task.Delay(15);
        return _accounts.Values.ToList();
    }

    public async Task<SubscriptionPlan?> GetSubscriptionPlanAsync(string planId)
    {
        Interlocked.Increment(ref _queryCount);
        await Task.Delay(5);
        _plans.TryGetValue(planId, out var plan);
        return plan;
    }

    public async Task UpdateSubscriptionPlanAsync(string userId, string newPlanId)
    {
        Interlocked.Increment(ref _queryCount);
        await Task.Delay(15); // Simulate ACID transaction commit
        if (_accounts.TryGetValue(userId, out var current))
        {
            var newRate = newPlanId == "plan-premium-4k" ? 19.99m : 12.99m;
            _accounts[userId] = current with { PlanId = newPlanId, MonthlyRate = newRate };
        }
    }

    public void ResetQueryCounter() => Interlocked.Exchange(ref _queryCount, 0);
}
