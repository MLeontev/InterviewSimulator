namespace InterviewSimulator.API.Swagger;

internal sealed class SwaggerOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string AuthorizationUrl { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = string.Empty;

    public Dictionary<string, string> Scopes { get; set; } = new()
    {
        ["openid"] = "OpenID Connect scope",
        ["profile"] = "User profile",
        ["email"] = "User email"
    };
}
