using Framework.Domain;
using MediatR;
using QuestionBank.UseCases.InterviewPreset.Queries;
using QuestionBank.UseCases.InterviewQuestion.Queries;

namespace QuestionBank.ModuleContract.Implementation;

internal class QuestionBankApi(ISender sender) : IQuestionBankApi
{
    public async Task<GeneratedQuestionSet> GenerateInterviewQuestionsAsync(
        Guid presetId, 
        int theoryCount, 
        int codingCount, 
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GenerateInterviewQuestionsQuery(presetId, theoryCount, codingCount), ct);

        if (result.IsFailure)
            throw new InvalidOperationException($"{result.Error.Code}: {result.Error.Description}");
        
        return MapToApi(result.Value);
    }
    
    public async Task<InterviewPresetApiDto?> GetPresetAsync(Guid interviewPresetId)
    {
        var result = await sender.Send(new GetInterviewPresetByIdQuery(interviewPresetId));

        if (result.IsFailure)
        {
            if (result.Error.Type == ErrorType.NotFound)
                return null;
            
            throw new InvalidOperationException($"{result.Error.Code}: {result.Error.Description}");
        }

        return new InterviewPresetApiDto(result.Value.Id, result.Value.Name);
    }
    
    public async Task<InterviewPresetDetailsApiDto?> GetPresetDetailsAsync(
        Guid interviewPresetId,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetInterviewPresetDetailsQuery(interviewPresetId), ct);

        if (result.IsFailure)
        {
            if (result.Error.Type == ErrorType.NotFound)
                return null;

            throw new InvalidOperationException($"{result.Error.Code}: {result.Error.Description}");
        }

        return new InterviewPresetDetailsApiDto(
            Id: result.Value.Id,
            Name: result.Value.Name,
            Technologies: result.Value.Technologies,
            Competencies: result.Value.Competencies
                .Select(c => new PresetCompetencyApiDto(c.CompetencyId, c.CompetencyName, c.Weight))
                .ToList());
    }
    
    private static GeneratedQuestionSet MapToApi(GeneratedQuestionSetDto set) =>
        new(
            PresetId: set.PresetId,
            Questions: set.Questions.Select(MapQuestion).ToList());

    private static GeneratedQuestion MapQuestion(GeneratedQuestionDto q) =>
        new(
            QuestionId: q.QuestionId,
            OrderIndex: q.OrderIndex,
            Type: MapType(q.Type),
            Title: q.Title,
            Text: q.Text,
            ReferenceSolution: q.ReferenceSolution,
            CompetencyId: q.CompetencyId,
            CompetencyName: q.CompetencyName,
            ProgrammingLanguageCode: q.ProgrammingLanguageCode,
            TimeLimitMs: q.TimeLimitMs,
            MemoryLimitMb: q.MemoryLimitMb,
            TestCases: q.TestCases.Select(MapTestCase).ToList());

    private static GeneratedTestCase MapTestCase(GeneratedTestCaseDto tc) =>
        new(
            Input: tc.Input,
            ExpectedOutput: tc.ExpectedOutput,
            IsHidden: tc.IsHidden,
            OrderIndex: tc.OrderIndex);

    private static QuestionType MapType(Domain.QuestionType type) =>
        type switch
        {
            Domain.QuestionType.Theory => QuestionType.Theory,
            Domain.QuestionType.Coding => QuestionType.Coding,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
