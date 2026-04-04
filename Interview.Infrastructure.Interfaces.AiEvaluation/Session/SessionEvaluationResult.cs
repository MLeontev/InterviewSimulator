namespace Interview.Infrastructure.Interfaces.AiEvaluation.Session;

public record SessionEvaluationResult(
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> Recommendations,
    string RawJson);