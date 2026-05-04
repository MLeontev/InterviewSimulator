namespace QuestionBank.Domain;

public class Technology
{
    public Guid Id { get; set; }
    public TechnologyCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Категория технологии
/// </summary>
public enum TechnologyCategory
{
    ProgrammingLanguage,
    Framework,
    ORM,
    Library,
    Tool,
    Database
}
