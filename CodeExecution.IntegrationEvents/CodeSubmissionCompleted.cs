namespace CodeExecution.IntegrationEvents;

public record CodeSubmissionCompleted(
    Guid SubmissionId,
    TestCaseResultDto[] TestCaseResults,
    Verdict OverallVerdict,
    int PassedCount,
    int TotalTests);
    
public record TestCaseResultDto(
    Guid TestCaseId,
    string Input,
    string ExpectedOutput,
    string ActualOutput,
    int Order,
    string Error,
    int ExitCode,
    double TimeElapsed,
    double MemoryUsage,
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