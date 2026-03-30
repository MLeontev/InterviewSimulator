namespace CodeExecution.Domain.Entities;

public class CodeSubmissionTestCase
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }
    public CodeSubmission Submission { get; set; } = null!;
    
    public int OrderIndex { get; set; }
    
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    
    public string? ActualOutput { get; set; }
    public string? Error { get; set; }

    public int? ExitCode { get; set; }
    public double? TimeElapsedMs { get; set; }
    public double? MemoryUsedMb { get; set; }

    public Verdict Verdict { get; set; } = Verdict.None;
}