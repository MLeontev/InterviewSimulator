namespace Interview.Infrastructure.Interfaces.AiEvaluation.Theory;

public record TheoryEvaluationResult(
    int Score,
    string Feedback,
    string RawJson);
