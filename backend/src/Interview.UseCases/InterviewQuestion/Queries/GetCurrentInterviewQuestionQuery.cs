using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.InterviewQuestion.Queries;

public record GetCurrentInterviewQuestionQuery(Guid CandidateId) : IRequest<Result<CurrentInterviewQuestion>>;

internal class GetCurrentInterviewQuestionQueryHandler(
    IDbContext dbContext,
    ICurrentSessionResolver currentSessionResolver) : IRequestHandler<GetCurrentInterviewQuestionQuery, Result<CurrentInterviewQuestion>>
{
    public async Task<Result<CurrentInterviewQuestion>> Handle(GetCurrentInterviewQuestionQuery request, CancellationToken ct)
    {
        var sessionId = await currentSessionResolver.GetCurrentSessionIdAsync(request.CandidateId, ct);
        if (sessionId is null)
            return Result.Failure<CurrentInterviewQuestion>(
                Error.NotFound("SESSION_NOT_FOUND", "Текущая сессия интервью не найдена"));
        
        var question = await dbContext.InterviewQuestions
            .AsNoTracking()
            .Where(x => x.InterviewSessionId == sessionId)
            .Where(x => !InterviewQuestionStatusRules.Terminal.Contains(x.Status))
            .OrderBy(x => x.OrderIndex)
            .Select(x => new CurrentInterviewQuestion
            {
                QuestionId = x.Id,
                OrderIndex = x.OrderIndex,
                Type = x.Type,
                Title = x.Title,
                Text = x.Text,
                Status = x.Status,
                Answer = x.Answer,
                ProgrammingLanguageCode = x.ProgrammingLanguageCode,
                TimeLimitMs = x.TimeLimitMs,
                MemoryLimitMb = x.MemoryLimitMb,
                OverallVerdict = x.OverallVerdict,
                ErrorMessage = x.ErrorMessage,
                TestCases = x.TestCases
                    .Where(tc => !tc.IsHidden)
                    .OrderBy(tc => tc.OrderIndex)
                    .Select(tc => new TestCaseDto
                    {
                        OrderIndex = tc.OrderIndex,
                        Input = tc.Input,
                        ExpectedOutput = tc.ExpectedOutput,
                        ActualOutput = tc.ActualOutput,
                        Verdict = tc.Verdict,
                        ExecutionTimeMs = tc.ExecutionTimeMs,
                        MemoryUsedMb = tc.MemoryUsedMb,
                        ErrorMessage = tc.ErrorMessage,
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (question is null)
            return Result.Failure<CurrentInterviewQuestion>(
                Error.NotFound("QUESTION_NOT_FOUND", "Текущее задание не найдено"));

        return Result.Success(question);
    }
}

public record CurrentInterviewQuestion
{
    public Guid QuestionId { get; init; }
    public int OrderIndex { get; init; }
    public QuestionType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string? ProgrammingLanguageCode { get; init; }
    public QuestionStatus Status { get; init; }
    public string? Answer { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitMb { get; init; }
    public Verdict OverallVerdict { get; init; }
    public string? ErrorMessage { get; init; }
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
    public string? ErrorMessage { get; init; }
}
