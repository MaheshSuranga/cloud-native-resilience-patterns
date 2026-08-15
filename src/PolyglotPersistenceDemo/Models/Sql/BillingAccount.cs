namespace PolyglotPersistenceDemo.Models.Sql;

public record BillingAccount(
    string AccountId,
    string UserId,
    string PlanId,
    string Status,
    decimal MonthlyRate,
    DateTimeOffset NextBillingDate
);
