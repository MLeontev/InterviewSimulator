namespace QuestionBank.Domain;

public class InterviewPresetCompetency
{
    public Guid InterviewPresetId { get; set; }
    public InterviewPreset InterviewPreset { get; set; } = null!;
    
    public Guid CompetencyId { get; set; }
    public Competency Competency { get; set; } = null!;
    
    public double Weight { get; set; }
}