using System.Text.Json;
using System.Text.Json.Serialization;

namespace Interview.UseCases.Services;

internal static class AiFeedbackJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryParseQuestion(string? rawJson, out int score, out string feedback)
    {
        score = 0;
        feedback = string.Empty;

        if (string.IsNullOrWhiteSpace(rawJson))
            return false;

        try
        {
            var result = JsonSerializer.Deserialize<QuestionAiFeedback>(rawJson, JsonOptions);
            if (result is null)
                return false;
            
            if (result.Score is null or < 0 or > 10)
                return false;

            score = result.Score.Value;
            feedback = result.Feedback?.Trim() ?? string.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseSession(
        string? rawJson,
        out string? summary,
        out IReadOnlyList<string> strengths,
        out IReadOnlyList<string> weaknesses,
        out IReadOnlyList<string> recommendations)
    {
        summary = null;
        strengths = [];
        weaknesses = [];
        recommendations = [];

        if (string.IsNullOrWhiteSpace(rawJson))
            return false;

        try
        {
            var result = JsonSerializer.Deserialize<SessionAiFeedback>(rawJson, JsonOptions);
            if (result is null)
                return false;
            
            summary = string.IsNullOrWhiteSpace(result.Summary) ? null : result.Summary.Trim();
            strengths = Normalize(result.Strengths);
            weaknesses = Normalize(result.Weaknesses);
            recommendations = Normalize(result.Recommendations);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private static IReadOnlyList<string> Normalize(List<string>? items) =>
        items?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList()
        ?? [];
    
    private record QuestionAiFeedback(
        [property: JsonPropertyName("score")] int? Score,
        [property: JsonPropertyName("feedback")] string? Feedback);

    private record SessionAiFeedback(
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("strengths")] List<string>? Strengths,
        [property: JsonPropertyName("weaknesses")] List<string>? Weaknesses,
        [property: JsonPropertyName("recommendations")] List<string>? Recommendations);
}