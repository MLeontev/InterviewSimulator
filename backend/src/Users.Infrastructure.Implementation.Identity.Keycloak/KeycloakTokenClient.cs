using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Users.Infrastructure.Implementation.Identity.Keycloak;

internal interface IKeycloakTokenClient
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

internal class KeycloakTokenClient(HttpClient httpClient, IOptions<KeycloakOptions> options) : IKeycloakTokenClient
{
    private readonly KeycloakOptions _keycloakOptions = options.Value;
    
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    
    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cachedToken is not null && now < _tokenExpiresAt.AddSeconds(-30))
            return _cachedToken;
        
        await _refreshLock.WaitAsync(ct);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (_cachedToken is not null && now < _tokenExpiresAt.AddSeconds(-30))
                return _cachedToken;
            
            var authRequestParameters = new Dictionary<string, string>
            {
                ["client_id"] = _keycloakOptions.ConfidentialClientId,
                ["client_secret"] = _keycloakOptions.ConfidentialClientSecret,
                ["grant_type"] = "client_credentials",
                ["scope"] = "openid"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _keycloakOptions.TokenUrl);
            request.Content = new FormUrlEncodedContent(authRequestParameters);

            using var response = await httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
        
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                        ?? throw new InvalidOperationException("Token response is null");
            
            _cachedToken = token.AccessToken;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);;
            return _cachedToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
    
    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}