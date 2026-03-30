namespace QuestionBank.Domain;

public class InterviewPreset
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid GradeId { get; set; }
    public Grade Grade { get; set; } = null!;

    public Guid SpecializationId { get; set; }
    public Specialization Specialization { get; set; } = null!;
    
    public List<InterviewPresetTechnology> Technologies { get; set; } = [];
    public List<InterviewPresetCompetency> InterviewPresetCompetencies { get; set; } = [];
}