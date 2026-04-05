namespace Interview.Infrastructure.Interfaces.AiEvaluation.Session;

public record SessionQuestionResult(
    string Title,
    string Status,
    int Score,
    string Feedback);