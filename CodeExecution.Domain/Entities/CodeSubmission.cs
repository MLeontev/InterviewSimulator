namespace CodeExecution.Domain.Entities;

public class CodeSubmission
{
    public Guid Id { get; set; }
    
    public Guid InterviewQuestionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    
    public int TimeLimitMs { get; set; }
    public int MemoryLimitMb { get; set; }

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;
    public Verdict OverallVerdict { get; set; } = Verdict.None;
    
    public string? ErrorMessage { get; set; }
    
    public List<CodeSubmissionTestCase> TestCases { get; set; } = [];
    
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public bool IsEventPublished { get; set; }
}

public enum ExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed
}