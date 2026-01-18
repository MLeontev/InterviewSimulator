namespace QuestionBank.Domain;

public enum QuestionType
{
    Coding,
    Theory
}

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

public class InterviewPresetTechnology
{
    public int InterviewPresetId { get; set; }
    public InterviewPreset InterviewPreset { get; set; } = null!;

    public int TechnologyId { get; set; }
    public Technology Technology { get; set; } = null!;
}