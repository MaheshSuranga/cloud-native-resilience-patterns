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

    public async Task<UserEntitlementDto> GetEntitlementsAsync(
        string userId,
        int? simulateDelay = null,
        bool? simulateError = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/entitlements/{Uri.EscapeDataString(userId)}";
        var queryParams = new List<string>();

        if (simulateDelay.HasValue && simulateDelay.Value > 0)
        {
            queryParams.Add($"simulateDelay={simulateDelay.Value}");
        }

        if (simulateError.HasValue && simulateError.Value)
        {
            queryParams.Add("simulateError=true");
        }

        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        _logger.LogInformation("Calling EntitlementsService for User '{UserId}' at {BaseAddress}{Url}", userId, _httpClient.BaseAddress, url);

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("EntitlementsService responded with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode(); // Throws HttpRequestException with status code to be recorded by Polly
        }

        var entitlement = await response.Content.ReadFromJsonAsync<UserEntitlementDto>(cancellationToken: cancellationToken);
        if (entitlement is null)
        {
            throw new InvalidOperationException($"Unable to deserialize entitlement payload for user '{userId}'");
        }

        return entitlement;
    }
}
