namespace QuestionBank.Domain;

public class CodingQuestionLanguageLimit
{
    public Guid Id { get; set; }

    public Guid CodingQuestionId { get; set; }
    public Question CodingQuestion { get; set; } = null!;

    public Guid LanguageId { get; set; }
    public Technology Language { get; set; } = null!;

    public int TimeLimitMs { get; set; }
    public int MemoryLimitMb { get; set; }
}