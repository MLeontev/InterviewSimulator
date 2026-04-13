namespace QuestionBank.Domain;

public class TestCase
{
    public Guid Id { get; set; }

    public Guid CodingQuestionId { get; set; }
    public Question CodingQuestion { get; set; } = null!;

    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    
    public int OrderIndex { get; set; }
}