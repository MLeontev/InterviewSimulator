namespace Interview.Infrastructure.Interfaces.AiEvaluation.Coding;

public record CodingEvaluationResult(
    int Score,
    string Feedback,
    string RawJson);