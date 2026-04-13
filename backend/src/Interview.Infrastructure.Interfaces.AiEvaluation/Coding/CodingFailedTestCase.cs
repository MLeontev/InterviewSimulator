namespace Interview.Infrastructure.Interfaces.AiEvaluation.Coding;

public record CodingFailedTestCase(
    string Input,
    string ExpectedOutput,
    string? ActualOutput,
    string Verdict,
    string? Error);