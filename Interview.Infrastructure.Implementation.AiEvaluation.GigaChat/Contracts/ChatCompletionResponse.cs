using System.Text.Json.Serialization;

namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat.Contracts;

internal record ChatCompletionResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("choices")]
    public IReadOnlyList<ChatChoice>? Choices { get; init; }
}