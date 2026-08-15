namespace RecommendationsService.Models;

public record PrecomputeRequest(
    string UserId,
    string Priority,
    DateTimeOffset QueuedAt
);
