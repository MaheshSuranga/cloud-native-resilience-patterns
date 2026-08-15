namespace RecommendationsService.Models;

public record HomepageRow(
    string RowId,
    string Title,
    string Category,
    IReadOnlyList<RecommendationItem> Items
);
