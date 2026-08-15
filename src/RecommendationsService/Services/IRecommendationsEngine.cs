using RecommendationsService.Models;

namespace RecommendationsService.Services;

public interface IRecommendationsEngine
{
    RecommendationsResponse GenerateRecommendations(UserEntitlementDto entitlement);
}
