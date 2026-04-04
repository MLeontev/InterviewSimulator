namespace Interview.Infrastructure.Interfaces.AiEvaluation.Theory;

public record TheoryEvaluationRequest(
    string QuestionText,
    string ReferenceSolution,
    string CandidateAnswer);