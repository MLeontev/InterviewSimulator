namespace Interview.Domain;

public class InterviewQuestion
{
    public Guid Id { get; set; }
    
    public Guid InterviewSessionId { get; set; }
    public InterviewSession InterviewSession { get; set; } = null!;
    
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    
    public int OrderIndex { get; set; }
    
    public string? TextAnswer { get; set; }
    public string? CodeAnswer { get; set; }
    
    public string? AiFeedbackJson { get; set; }
    
    public QuestionStatus Status { get; set; }
    
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    
    public List<TestCase> TestCases { get; set; } = [];
}

public enum QuestionType
{
    Coding,
    Theory
}

public enum QuestionStatus
{
    NotStarted,
    InProgress,
    Submitted,
    Evaluated
}