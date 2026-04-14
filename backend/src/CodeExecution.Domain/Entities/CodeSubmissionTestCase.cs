using CodeExecution.Domain.Enums;

namespace CodeExecution.Domain.Entities;

public class CodeSubmissionTestCase
{
    public Guid Id { get; private set; }

    public Guid SubmissionId { get; private set; }
    public CodeSubmission Submission { get; private set; } = null!;
    
    public Guid InterviewTestCaseId { get; private set; }
    
    public int OrderIndex { get; private set; }
    
    public string Input { get; private set; } = string.Empty;
    public string ExpectedOutput { get; private set; } = string.Empty;
    
    public string? ActualOutput { get; private set; }
    public string? Error { get; private set; }

    public int? ExitCode { get; private set; }
    public double? TimeElapsedMs { get; private set; }
    public double? MemoryUsedMb { get; private set; }

    public Verdict Verdict { get; private set; } = Verdict.None;

    public static CodeSubmissionTestCase Create(
        Guid interviewTestCaseId,
        int orderIndex,
        string input,
        string expectedOutput)
    {
        return new CodeSubmissionTestCase
        {
            Id = Guid.NewGuid(),
            InterviewTestCaseId = interviewTestCaseId,
            OrderIndex = orderIndex,
            Input = input,
            ExpectedOutput = expectedOutput,
            ActualOutput = null,
            Error = null,
            ExitCode = null,
            TimeElapsedMs = null,
            MemoryUsedMb = null,
            Verdict = Verdict.None
        };
    }

    internal void AttachToSubmission(Guid submissionId)
    {
        SubmissionId = submissionId;
    }

    public void ApplyExecutionResult(
        string actualOutput,
        string? error,
        int exitCode,
        double timeElapsedMs,
        double memoryUsedMb,
        Verdict verdict)
    {
        ActualOutput = actualOutput;
        Error = string.IsNullOrWhiteSpace(error) ? null : error;
        ExitCode = exitCode;
        TimeElapsedMs = timeElapsedMs;
        MemoryUsedMb = memoryUsedMb;
        Verdict = verdict;
    }
}
