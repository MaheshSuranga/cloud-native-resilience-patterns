namespace RecommendationsService.Models;

public record HomepageLayoutResponse(
    string UserId,
    string LayoutType,
    string Source,
    DateTimeOffset GeneratedAt,
    HomepageHeroBanner Hero,
    IReadOnlyList<HomepageRow> Rows
);
