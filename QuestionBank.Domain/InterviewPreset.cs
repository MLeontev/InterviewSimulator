namespace QuestionBank.Domain;

public class InterviewPreset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;

    public int SpecializationId { get; set; }
    public Specialization Specialization { get; set; } = null!;
    
    public List<InterviewPresetTechnology> Technologies { get; set; } = [];
}