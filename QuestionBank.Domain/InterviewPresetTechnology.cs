namespace QuestionBank.Domain;

public class InterviewPresetTechnology
{
    public int InterviewPresetId { get; set; }
    public InterviewPreset InterviewPreset { get; set; } = null!;

    public int TechnologyId { get; set; }
    public Technology Technology { get; set; } = null!;
}