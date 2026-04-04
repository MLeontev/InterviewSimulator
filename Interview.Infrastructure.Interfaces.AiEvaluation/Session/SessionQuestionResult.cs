namespace Interview.Infrastructure.Interfaces.AiEvaluation.Session;

public record SessionQuestionResult(
    int OrderIndex,
    string Type,
    string Title,
    string Status,
    int Score,
    string Feedback);