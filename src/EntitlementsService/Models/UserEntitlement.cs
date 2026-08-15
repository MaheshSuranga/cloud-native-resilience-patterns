namespace EntitlementsService.Models;

public record UserEntitlement(
    string UserId,
    bool IsPremium,
    string Tier,
    IReadOnlyList<string> ActiveFeatures
);
