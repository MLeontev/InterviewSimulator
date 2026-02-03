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
            InterviewSessionId = question.InterviewSessionId,
            Text = question.Text,
            Type = question.Type,
            ProgrammingLanguageCode = question.ProgrammingLanguageCode,
            OrderIndex = question.OrderIndex,
            Status = question.Status,
            Answer = question.Answer,
            TimeLimitMs = question.TimeLimitMs,
            MemoryLimitMb = question.MemoryLimitMb,
            TestCases = question.TestCases
                .OrderBy(tc => tc.OrderIndex)
                .Where(tc => !tc.IsHidden)
                .Select(tc => new TestCaseDto
                {
                    Id = tc.Id,
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    ActualOutput = tc.ActualOutput,
                    Verdict = tc.Verdict,
                    ExecutionTimeMs = tc.ExecutionTimeMs,
                    MemoryUsedKb = tc.MemoryUsedKb
                })
                .ToList()
        };

        return Result.Success(dto);
    }
}

public record InterviewQuestionDto
{
    public Guid Id { get; init; }
    public Guid InterviewSessionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public string? ProgrammingLanguageCode { get; init; }
    public int OrderIndex { get; init; }
    public QuestionStatus Status { get; init; }
    public string? Answer { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitMb { get; init; }
    public IReadOnlyList<TestCaseDto> TestCases { get; init; } = [];
}

public record TestCaseDto
{
    public Guid Id { get; init; }
    public string Input { get; init; } = string.Empty;
    public string ExpectedOutput { get; init; } = string.Empty;
    public string? ActualOutput { get; init; }
    public Verdict Verdict { get; init; }
    public double? ExecutionTimeMs { get; init; }
    public double? MemoryUsedKb { get; init; }
}