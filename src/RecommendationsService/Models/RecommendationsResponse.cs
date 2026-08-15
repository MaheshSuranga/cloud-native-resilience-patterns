namespace RecommendationsService.Models;

public record RecommendationsResponse(
    string UserId,
    string Tier,
    string Source,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<RecommendationItem> Items
);
