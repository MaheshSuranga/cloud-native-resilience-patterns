namespace RecommendationsService.Models;

public record DegradedResponse(
    string Status,
    string Reason,
    int RetryAfterSeconds,
    string? Message = null
);
