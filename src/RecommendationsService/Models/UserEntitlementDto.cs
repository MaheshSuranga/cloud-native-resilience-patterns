namespace RecommendationsService.Models;

public record UserEntitlementDto(
    string UserId,
    bool IsPremium,
    string Tier,
    IReadOnlyList<string> ActiveFeatures
);
