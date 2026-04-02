using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Queries;

public record GetInterviewQuestionQuery(Guid QuestionId) : IRequest<Result<InterviewQuestionDto>>;

internal class GetInterviewQuestionQueryHandler(IDbContext dbContext) : IRequestHandler<GetInterviewQuestionQuery, Result<InterviewQuestionDto>>
{
    public async Task<Result<InterviewQuestionDto>> Handle(GetInterviewQuestionQuery request, CancellationToken ct)
    {
        var question = await dbContext.InterviewQuestions
            .Include(q => q.TestCases)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct);

        if (question == null)
            return Result.Failure<InterviewQuestionDto>(
                Error.NotFound("QUESTION_NOT_FOUND", "Вопрос не найден"));

        var dto = new InterviewQuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Text = question.Text,
            Type = question.Type,
            ProgrammingLanguageCode = question.ProgrammingLanguageCode,
            OrderIndex = question.OrderIndex,
            Status = question.Status,
            Answer = question.Answer,
            OverallVerdict = question.OverallVerdict,
            ErrorMessage = question.ErrorMessage,
            SubmittedAt = question.SubmittedAt,
            EvaluatedAt = question.EvaluatedAt,
            TimeLimitMs = question.TimeLimitMs,
            MemoryLimitMb = question.MemoryLimitMb,
            TestCases = question.TestCases
                .OrderBy(tc => tc.OrderIndex)
                .Where(tc => !tc.IsHidden)
                .Select(tc => new TestCaseDto
                {
                    OrderIndex = tc.OrderIndex,
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    ActualOutput = tc.ActualOutput,
                    Verdict = tc.Verdict,
                    ExecutionTimeMs = tc.ExecutionTimeMs,
                    MemoryUsedMb = tc.MemoryUsedMb
                })
                .ToList()
        };

        return Result.Success(dto);
    }
}

public record InterviewQuestionDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public string? ProgrammingLanguageCode { get; init; }
    public int OrderIndex { get; init; }
    public QuestionStatus Status { get; init; }
    public string? Answer { get; init; }
    public Verdict OverallVerdict { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? EvaluatedAt { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitMb { get; init; }
    public IReadOnlyList<TestCaseDto> TestCases { get; init; } = [];
}

public record TestCaseDto
{
    public int OrderIndex { get; init; }
    public string Input { get; init; } = string.Empty;
    public string ExpectedOutput { get; init; } = string.Empty;
    public string? ActualOutput { get; init; }
    public Verdict Verdict { get; init; }
    public double? ExecutionTimeMs { get; init; }
    public double? MemoryUsedMb { get; init; }
}
