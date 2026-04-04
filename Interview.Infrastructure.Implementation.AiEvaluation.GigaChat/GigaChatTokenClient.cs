using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat;

internal interface IGigaChatTokenClient
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

internal class GigaChatTokenClient(
    HttpClient httpClient, 
    IOptions<GigaChatAuthOptions> options,
    ILogger<GigaChatTokenClient> logger) : IGigaChatTokenClient
{
    private readonly GigaChatAuthOptions _options = options.Value;
    
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (HasValidToken())
            return _cachedToken!;
        
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (HasValidToken())
                return _cachedToken!;
            
            var token = await RequestNewTokenAsync(cancellationToken);

            _cachedToken = token.AccessToken;
            _tokenExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(token.ExpiresAt);
            
            return _cachedToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool HasValidToken()
    {
        if (string.IsNullOrEmpty(_cachedToken))
            return false;
        
        return DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-_options.RefreshSeconds);
    }

    private async Task<TokenResponse> RequestNewTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.OAuthUrl);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["scope"] = _options.Scope
        });
        
        var rqUid = Guid.NewGuid().ToString();
        request.Headers.Add("RqUID", rqUid);
        
        var raw = $"{_options.ClientId}:{_options.ClientSecret}";
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        
        logger.LogInformation(
            "Sending GigaChat OAuth request. Url: {Url}, Scope: {Scope}, RqUID: {RqUID}",
            _options.OAuthUrl,
            _options.Scope,
            rqUid);
        
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogInformation(
            "GigaChat OAuth response received. StatusCode: {StatusCode}",
            (int)response.StatusCode);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "GigaChat OAuth request failed. StatusCode: {StatusCode}, ResponseBody: {ResponseBody}",
                (int)response.StatusCode,
                body);

            response.EnsureSuccessStatusCode();
        }
        
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                      ?? throw new InvalidOperationException("GigaChat token response is empty.");

        return payload;
    }
    
    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; init; }
    }
}