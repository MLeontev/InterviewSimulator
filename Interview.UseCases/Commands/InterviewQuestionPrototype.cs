using Framework.Domain;
using Interview.Domain;
using QuestionBank.InternalApi;
using QuestionType = Interview.Domain.QuestionType;

namespace Interview.UseCases.Commands;

internal sealed class InterviewQuestionPrototype : IPrototype<InterviewQuestion>
{
    private readonly InterviewQuestion _template;
    
    public InterviewQuestionPrototype(InterviewQuestionApiDto dto)
    {
        _template = new InterviewQuestion
        {
            Text = dto.Text,
            Type = MapQuestionType(dto.Type),
            ProgrammingLanguageCode = dto.ProgrammingLanguageCode,
            ReferenceSolution = dto.ReferenceSolution,
            Status = QuestionStatus.NotStarted,
            OverallVerdict = Verdict.None,
            TimeLimitMs = dto.TimeLimitMs,
            MemoryLimitMb = dto.MemoryLimitMb,
            TestCases = dto.TestCases
                .Select((tc, index) => new TestCase
                {
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsHidden = tc.IsHidden,
                    OrderIndex = index,
                    ActualOutput = null,
                    ExecutionTimeMs = null,
                    MemoryUsedKb = null,
                    Verdict = Verdict.None
                })
                .ToList()
        };
    }
    
    public InterviewQuestion Clone()
    {
        var questionId = Guid.NewGuid();

        return new InterviewQuestion
        {
            Id = questionId,
            Text = _template.Text,
            Type = _template.Type,
            ProgrammingLanguageCode = _template.ProgrammingLanguageCode,
            ReferenceSolution = _template.ReferenceSolution,
            Status = _template.Status,
            OverallVerdict = _template.OverallVerdict,
            TimeLimitMs = _template.TimeLimitMs,
            MemoryLimitMb = _template.MemoryLimitMb,
            TestCases = _template.TestCases
                .Select(tc => new TestCase
                {
                    Id = Guid.NewGuid(),
                    InterviewQuestionId = questionId,
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsHidden = tc.IsHidden,
                    OrderIndex = tc.OrderIndex,
                    ActualOutput = null,
                    ExecutionTimeMs = null,
                    MemoryUsedKb = null,
                    Verdict = Verdict.None
                })
                .ToList()
        };
    }
    
    public InterviewQuestion CloneForSession(Guid sessionId, int orderIndex)
    {
        var clone = Clone();
        clone.InterviewSessionId = sessionId;
        clone.OrderIndex = orderIndex;
        return clone;
    }
    
    private static QuestionType MapQuestionType(QuestionBank.InternalApi.QuestionType type)
        => type switch
        {
            QuestionBank.InternalApi.QuestionType.Coding => QuestionType.Coding,
            QuestionBank.InternalApi.QuestionType.Theory => QuestionType.Theory,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}