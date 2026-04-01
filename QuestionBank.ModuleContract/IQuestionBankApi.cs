namespace QuestionBank.ModuleContract;

public interface IQuestionBankApi
{
    Task<GeneratedQuestionSet> GenerateInterviewQuestionsAsync(
        Guid presetId,
        int theoryCount,
        int codingCount,
        CancellationToken ct = default);
    
    Task<InterviewPresetApiDto?> GetPresetAsync(Guid interviewPresetId);
}

public record GeneratedQuestionSet(
    Guid PresetId,
    IReadOnlyCollection<GeneratedQuestion> Questions);

public record GeneratedQuestion(
    Guid QuestionId,
    int OrderIndex,
    QuestionType Type,
    string Title,
    string Text,
    string ReferenceSolution,
    string? ProgrammingLanguageCode,
    int? TimeLimitMs,
    int? MemoryLimitMb,
    IReadOnlyCollection<GeneratedTestCase> TestCases);

public record GeneratedTestCase(
    string Input,
    string ExpectedOutput,
    bool IsHidden,
    int OrderIndex);

public enum QuestionType
{
    Theory,
    Coding
}

public record InterviewPresetApiDto(Guid Id, string Name);
