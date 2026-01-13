namespace CodeExecution.Domain.Entities;

public class CodeSubmission
{
    public Guid Id { get; set; }
    
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    
    public int? MaxTimeSeconds { get; set; }
    public int? MaxMemoryMb { get; set; }
    
    public ExecutionStatus Status { get; set; }
    
    public List<CodeSubmissionTestCase> TestCases { get; set; } = [];
    
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}