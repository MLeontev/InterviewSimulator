namespace QuestionBank.Domain;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    
    public string ReferenceSolution { get; set; } = string.Empty;
    
    public int CompetencyId { get; set; }
    public Competency Competency { get; set; } = null!;

    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;

    public int TechnologyId { get; set; }
    public Technology? Technology { get; set; }
    
    public List<CodingQuestionLanguageLimit> LanguageLimits { get; set; } = [];
    public List<TestCase> TestCases { get; set; } = [];
}