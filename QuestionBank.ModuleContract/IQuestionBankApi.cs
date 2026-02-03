namespace QuestionBank.InternalApi;

public interface IQuestionBankApi
{
    Task<IReadOnlyList<InterviewQuestionApiDto>> GetQuestionsAsync(Guid interviewPresetId, int totalQuestions);
    Task<InterviewPresetApiDto?> GetPresetAsync(Guid interviewPresetId);
}

public record InterviewPresetApiDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record InterviewQuestionApiDto
{
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public int OrderIndex { get; init; }
    public string? ProgrammingLanguageCode { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitMb { get; init; }
    public string ReferenceSolution { get; init; } = string.Empty;
    public List<TestCaseApiDto> TestCases { get; init; } = [];
}

public record TestCaseApiDto
{
    public string Input { get; init; } = string.Empty;
    public string ExpectedOutput { get; init; } = string.Empty;
    public bool IsHidden { get; init; }
}

public enum QuestionType
{
    Theory,
    Coding
}