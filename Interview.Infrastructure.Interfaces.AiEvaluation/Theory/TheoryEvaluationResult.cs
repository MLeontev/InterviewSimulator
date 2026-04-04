namespace Interview.Infrastructure.Interfaces.AiEvaluation.Theory;

public sealed record TheoryEvaluationResult(
    int Score,
    string Feedback,
    string RawJson);