namespace CodeExecution.IntegrationEvents;

public record CodeSubmissionCompleted(
    Guid SubmissionId,
    string Code,
    string Language,
    TestCaseResultDto[] TestCaseResults,
    string OverallVerdict);
    
public record TestCaseResultDto(
    string Input,
    string ExpectedOutput,
    string ActualOutput,
    string Error,
    int ExitCode,
    double TimeElapsed,
    double MemoryUsage,
    string Verdict);