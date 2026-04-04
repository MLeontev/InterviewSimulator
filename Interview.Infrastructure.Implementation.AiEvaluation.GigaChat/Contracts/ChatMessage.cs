using System.Text.Json.Serialization;

namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat.Contracts;

internal record ChatMessage
{
    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }

    [JsonPropertyName("role")]
    public string Role { get; init; }

    [JsonPropertyName("content")]
    public string Content { get; init; }
}