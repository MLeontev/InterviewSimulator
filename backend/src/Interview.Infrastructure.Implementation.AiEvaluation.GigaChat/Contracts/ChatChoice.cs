using System.Text.Json.Serialization;

namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat.Contracts;

internal record class ChatChoice
{
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; init; }
}