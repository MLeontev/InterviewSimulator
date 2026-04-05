namespace QuestionBank.ModuleContract;

public interface IQuestionBankApi
{
    Task<GeneratedQuestionSet> GenerateInterviewQuestionsAsync(
        Guid presetId,
        int theoryCount,
        int codingCount,
        CancellationToken ct = default);
    
    Task<InterviewPresetApiDto?> GetPresetAsync(Guid interviewPresetId);
    
    Task<InterviewPresetDetailsApiDto?> GetPresetDetailsAsync(
        Guid interviewPresetId,
        CancellationToken ct = default);
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
    Guid CompetencyId,
    string CompetencyName,
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

public record InterviewPresetDetailsApiDto(
    Guid Id,
    string Name,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<PresetCompetencyApiDto> Competencies);

public record PresetCompetencyApiDto(
    Guid CompetencyId,
    string CompetencyName,
    double Weight);
