namespace RecommendationsService.Models;

public record RecommendationItem(
    string Id,
    string Title,
    string Genre,
    string Quality,
    double Score,
    string Description,
    string PosterUrl
);
