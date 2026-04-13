namespace CodeExecution.IntegrationEvents;

public record CodeSubmissionCompleted(
    Guid SubmissionId,
    Guid InterviewQuestionId,
    IReadOnlyList<TestCaseResultDto> TestCaseResults,
    Verdict OverallVerdict,
    int PassedCount,
    int TotalTests,
    string? ErrorMessage = null);
    
public record TestCaseResultDto(
    Guid InterviewTestCaseId,
    int OrderIndex,
    string Input,
    string ExpectedOutput,
    string ActualOutput,
    string Error,
    int ExitCode,
    double TimeElapsedMs,
    double MemoryUsedMb,
    Verdict Verdict);
    
public enum Verdict
{
    FailedSystem,
    OK,
    CE,
    RE,
    TLE,
    MLE,
    WA
}