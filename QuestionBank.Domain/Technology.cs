namespace QuestionBank.Domain;

public class Technology
{
    public int Id { get; set; }
    public TechnologyCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
}

public enum TechnologyCategory
{
    ProgrammingLanguage,
    Framework,
    ORM,
    Library,
    Tool,
    Database
}