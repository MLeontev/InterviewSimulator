namespace Interview.Domain;

public class InterviewSession
{
    public Guid Id { get; set; }
    
    public Guid CandidateId { get; set; }
    
    public Guid InterviewPresetId { get; set; }
    public string InterviewPresetName { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }
    public DateTime PlannedEndAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public InterviewStatus Status { get; set; }
    public SessionVerdict SessionVerdict { get; set; } = SessionVerdict.None;
    
    public string? AiFeedbackJson { get; set; }
    public int AiRetryCount { get; set; } = 0;
    public DateTime? AiNextRetryAt { get; set; }

    public List<InterviewQuestion> Questions { get; set; } = [];
}

public enum InterviewStatus
{
    InProgress,
    Finished,
    EvaluatingAi,
    Evaluated
}

public enum SessionVerdict
{
    None,
    Passed,
    Borderline,
    Failed
}
