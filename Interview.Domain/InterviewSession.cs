namespace Interview.Domain;

public class InterviewSession
{
    public Guid Id { get; set; }
    
    public Guid CandidateId { get; set; }
    
    public string InterviewPresetName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public InterviewStatus Status { get; set; }
    
    public string? AiFeedbackJson { get; set; }

    public List<InterviewQuestion> Questions { get; set; } = [];
}

public enum InterviewStatus
{
    Created,
    InProgress,
    Finished,
    Cancelled
}