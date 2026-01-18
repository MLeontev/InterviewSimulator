namespace QuestionBank.Domain;

public class CodingQuestionLanguageLimit
{
    public int Id { get; set; }

    public int CodingQuestionId { get; set; }
    public Question CodingQuestion { get; set; } = null!;

    public int LanguageId { get; set; }
    public Technology Language { get; set; } = null!;

    public int TimeLimitMs { get; set; }
    public int MemoryLimitMb { get; set; }
}