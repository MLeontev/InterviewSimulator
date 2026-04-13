namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat;

internal class GigaChatAuthOptions
{
    public string OAuthUrl { get; set; } = string.Empty;
    
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    
    public string Scope { get; set; } = "GIGACHAT_API_PERS";
    
    public int RefreshSeconds { get; set; } = 60;
}