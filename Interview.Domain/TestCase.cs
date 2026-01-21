namespace Interview.Domain;

public class TestCase
{
    public Guid Id { get; set; }

    public int InterviewQuestionId { get; set; }
    public InterviewQuestion InterviewQuestion { get; set; } = null!;

    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    
    public int OrderIndex { get; set; }
    
    public string ActualOutput { get; set; } = string.Empty;
    public double ExecutionTimeMs { get; set; }
    public double MemoryUsedKb { get; set; }
    public Verdict Verdict { get; set; } = Verdict.None;
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