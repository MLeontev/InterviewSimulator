using CodeExecution.Domain.Enums;

namespace CodeExecution.Domain.Entities;

public class CodeSubmission
{
    private readonly List<CodeSubmissionTestCase> _testCases = [];
    public IReadOnlyList<CodeSubmissionTestCase> TestCases => _testCases;
    
    public Guid Id { get; private set; }
    
    public Guid InterviewQuestionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = string.Empty;
    
    public int TimeLimitMs { get; private set; }
    public int MemoryLimitMb { get; private set; }

    public ExecutionStatus Status { get; private set; } = ExecutionStatus.Pending;
    public Verdict OverallVerdict { get; private set; } = Verdict.None;
    
    public string? ErrorMessage { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static CodeSubmission Create(
        Guid id,
        Guid interviewQuestionId,
        string code,
        string languageCode,
        DateTime createdAtUtc,
        int? timeLimitMs,
        int? memoryLimitMb,
        IReadOnlyCollection<CodeSubmissionTestCase>? testCases = null)
    {
        var submission = new CodeSubmission
        {
            Id = id,
            InterviewQuestionId = interviewQuestionId,
            Code = code,
            LanguageCode = languageCode,
            TimeLimitMs = timeLimitMs ?? 5_000,
            MemoryLimitMb = memoryLimitMb ?? 64,
            Status = ExecutionStatus.Pending,
            OverallVerdict = Verdict.None,
            ErrorMessage = null,
            CreatedAt = createdAtUtc,
            StartedAt = null,
            CompletedAt = null
        };

        if (testCases is not null)
        {
            foreach (var testCase in testCases.OrderBy(x => x.OrderIndex))
            {
                testCase.AttachToSubmission(id);
                submission._testCases.Add(testCase);
            }
        }

        return submission;
    }

    public void MarkStarted(DateTime nowUtc)
    {
        StartedAt ??= nowUtc;
    }

    public void Complete(Verdict overallVerdict, string? errorMessage, DateTime nowUtc)
    {
        OverallVerdict = overallVerdict;
        CompletedAt = nowUtc;
        ErrorMessage = errorMessage;
        Status = overallVerdict == Verdict.FailedSystem
            ? ExecutionStatus.Failed
            : ExecutionStatus.Completed;
    }
}
