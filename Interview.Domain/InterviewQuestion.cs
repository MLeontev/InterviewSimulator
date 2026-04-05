namespace Interview.Domain;

public class InterviewQuestion
{
    public Guid Id { get; set; }
    
    public Guid InterviewSessionId { get; set; }
    public InterviewSession InterviewSession { get; set; } = null!;
    
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Guid? CompetencyId { get; set; }
    public string? CompetencyName { get; set; }
    
    public int OrderIndex { get; set; }
    
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public QuestionStatus Status { get; set; } = QuestionStatus.NotStarted;
    
    public string? Answer { get; set; }
    public string ReferenceSolution { get; set; } = string.Empty;
    public string? AiFeedbackJson { get; set; }
    public string? ErrorMessage { get; set; }
    
    public Guid? LastSubmissionId { get; set; }
    public string? ProgrammingLanguageCode { get; set; }
    public QuestionVerdict QuestionVerdict { get; set; } = QuestionVerdict.None;
    public Verdict OverallVerdict { get; set; } = Verdict.None;
    public int? TimeLimitMs { get; set; }
    public int? MemoryLimitMb { get; set; }
    public List<TestCase> TestCases { get; set; } = [];
}

public enum QuestionType
{
    Theory,
    Coding
}

public enum QuestionStatus
{
    NotStarted,
    InProgress,
    Skipped,
    Submitted,
    EvaluatingCode,
    EvaluatedCode,
    EvaluatingAi,
    EvaluatedAi
}

public enum QuestionVerdict
{
    None,
    Correct,
    PartiallyCorrect,
    Incorrect
}
