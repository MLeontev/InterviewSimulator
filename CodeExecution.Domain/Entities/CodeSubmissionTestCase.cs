namespace CodeExecution.Domain.Entities;

public class CodeSubmissionTestCase
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }
    public CodeSubmission Submission { get; set; } = null!;
    
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    
    public string ActualOutput { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;

    public int ExitCode { get; set; }
    public double TimeElapsed { get; set; }
    public double MemoryUsage { get; set; }

    public Verdict Verdict { get; set; }
}