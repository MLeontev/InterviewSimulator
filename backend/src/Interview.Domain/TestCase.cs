namespace Interview.Domain;

public class TestCase
{
    public Guid Id { get; set; }

    public Guid InterviewQuestionId { get; set; }
    public InterviewQuestion InterviewQuestion { get; set; } = null!;

    public int OrderIndex { get; set; }
    
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; }

    public string? ActualOutput { get; set; }
    public double? ExecutionTimeMs { get; set; }
    public double? MemoryUsedMb { get; set; }
    public Verdict Verdict { get; set; } = Verdict.None;
    public string? ErrorMessage { get; set; }
}

public enum Verdict
{
    None,
    FailedSystem,
    OK,
    CE,
    RE,
    TLE,
    MLE,
    WA
}