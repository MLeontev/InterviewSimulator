using MediatR;
using QuestionBank.InternalApi;
using QuestionBank.UseCases.Queries;

namespace QuestionBank.ModuleContract.Implementation;

internal class QuestionBankApi(ISender sender) : IQuestionBankApi
{
    public async Task<IReadOnlyList<InterviewQuestionApiDto>> GetQuestionsAsync(Guid interviewPresetId, int totalQuestions)
    {
        var result = await sender.Send(new GenerateInterviewQuestionsQuery(interviewPresetId, totalQuestions));

        if (result.IsFailure)
            return [];

        var mapped = result.Value.Select(q => new InterviewQuestionApiDto
        {
            Text = q.Text,
            Type = MapQuestionType(q.Type),
            OrderIndex = q.OrderIndex,
            ProgrammingLanguageCode = q.ProgrammingLanguageCode,
            TimeLimitMs = q.TimeLimitMs,
            MemoryLimitMb = q.MemoryLimitMb,
            ReferenceSolution = q.ReferenceSolution,
            TestCases = q.TestCases
                .Select(tc => new TestCaseApiDto
                {
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsHidden = tc.IsHidden
                })
                .ToList()
        }).ToList();

        return mapped;
    }

    public async Task<InterviewPresetApiDto?> GetPresetAsync(Guid interviewPresetId)
    {
        var result = await sender.Send(new GetInterviewPresetByIdQuery(interviewPresetId));

        if (result.IsFailure)
            return null;

        return new InterviewPresetApiDto
        {
            Id = result.Value.Id,
            Name = result.Value.Name
        };
    }

    private QuestionType MapQuestionType(Domain.QuestionType type)
        => type switch
        {
            Domain.QuestionType.Coding => QuestionType.Coding,
            Domain.QuestionType.Theory => QuestionType.Theory,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}