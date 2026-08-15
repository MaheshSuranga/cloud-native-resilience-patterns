using RecommendationsService.Models;

namespace RecommendationsService.Clients;

public interface IEntitlementsClient
{
    Task<UserEntitlementDto> GetEntitlementsAsync(string userId, CancellationToken cancellationToken = default);
}
