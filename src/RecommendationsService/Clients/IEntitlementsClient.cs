using RecommendationsService.Models;

namespace RecommendationsService.Clients;

public interface IEntitlementsClient
{
    Task<UserEntitlementDto> GetEntitlementsAsync(
        string userId,
        int? simulateDelay = null,
        bool? simulateError = null,
        CancellationToken cancellationToken = default);
}
