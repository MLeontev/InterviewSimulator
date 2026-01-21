namespace QuestionBank.Domain;

public class InterviewPresetTechnology
{
    public Guid InterviewPresetId { get; set; }
    public InterviewPreset InterviewPreset { get; set; } = null!;

    public Guid TechnologyId { get; set; }
    public Technology Technology { get; set; } = null!;
}