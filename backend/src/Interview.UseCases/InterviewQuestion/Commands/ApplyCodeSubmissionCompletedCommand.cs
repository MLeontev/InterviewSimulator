using Framework.Domain;
using Interview.Domain.Models;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Verdict = Interview.Domain.Enums.Verdict;

namespace Interview.UseCases.InterviewQuestion.Commands;

public record ApplyCodeSubmissionCompletedCommand(
    Guid SubmissionId,
    Guid InterviewQuestionId,
    IReadOnlyList<ApplyCodeSubmissionTestCaseResultDto> TestCaseResults,
    Verdict OverallVerdict,
    string? ErrorMessage = null) : IRequest<Result>;

public record ApplyCodeSubmissionTestCaseResultDto(
    Guid InterviewTestCaseId,
    string ActualOutput,
    string Error,
    double TimeElapsedMs,
    double MemoryUsedMb,
    Verdict Verdict);

internal sealed class ApplyCodeSubmissionCompletedCommandHandler(IDbContext dbContext)
    : IRequestHandler<ApplyCodeSubmissionCompletedCommand, Result>
{
    public async Task<Result> Handle(ApplyCodeSubmissionCompletedCommand request, CancellationToken ct)
    {
        var question = await dbContext.InterviewQuestions
            .Include(q => q.TestCases)
            .FirstOrDefaultAsync(q => q.Id == request.InterviewQuestionId, ct);

        if (question == null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание для результата проверки не найдено"));

        var result = question.ApplyCodeSubmissionResult(
            submissionId: request.SubmissionId,
            testCaseResults: request.TestCaseResults
                .Select(x => new CodeCheckTestCaseResult(
                    InterviewTestCaseId: x.InterviewTestCaseId,
                    ActualOutput: x.ActualOutput,
                    ErrorMessage: x.Error,
                    TimeElapsedMs: x.TimeElapsedMs,
                    MemoryUsedMb: x.MemoryUsedMb,
                    Verdict: x.Verdict))
                .ToList(),
            overallVerdict: request.OverallVerdict,
            nowUtc: DateTime.UtcNow,
            errorMessage: request.ErrorMessage);
        
        if (result.IsFailure) return Result.Failure(result.Error);
        
        if (!result.Value) return Result.Success();

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
