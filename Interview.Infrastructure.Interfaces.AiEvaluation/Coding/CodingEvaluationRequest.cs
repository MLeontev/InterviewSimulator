namespace Interview.Infrastructure.Interfaces.AiEvaluation.Coding;

public record CodingEvaluationRequest(string QuestionText,
    string ReferenceSolution,
    string CandidateCode,
    string OverallVerdict,
    int PassedCount,
    int TotalTests,
    CodingFailedTestCase? FirstFailedTest = null);