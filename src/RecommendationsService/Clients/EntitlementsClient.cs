using System.Net.Http.Json;
using RecommendationsService.Models;

namespace RecommendationsService.Clients;

public class EntitlementsClient : IEntitlementsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EntitlementsClient> _logger;

    public EntitlementsClient(HttpClient httpClient, ILogger<EntitlementsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserEntitlementDto> GetEntitlementsAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling EntitlementsService for User '{UserId}' at {BaseAddress}", userId, _httpClient.BaseAddress);

        var response = await _httpClient.GetAsync($"/entitlements/{userId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("EntitlementsService responded with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode(); // Throws HttpRequestException with status code
        }

        var entitlement = await response.Content.ReadFromJsonAsync<UserEntitlementDto>(cancellationToken: cancellationToken);
        if (entitlement is null)
        {
            throw new InvalidOperationException($"Unable to deserialize entitlement payload for user '{userId}'");
        }

        return entitlement;
    }
}
