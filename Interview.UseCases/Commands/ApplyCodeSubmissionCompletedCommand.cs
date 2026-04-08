using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Verdict = Interview.Domain.Verdict;

namespace Interview.UseCases.Commands;

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

        if (question.LastSubmissionId != request.SubmissionId 
            || question.Status != QuestionStatus.EvaluatingCode)
            return Result.Success();

        question.OverallVerdict = request.OverallVerdict;
        question.EvaluatedAt = DateTime.UtcNow;
        question.Status = QuestionStatus.EvaluatedCode;
        
        var total = request.TestCaseResults.Count;
        var passed = request.TestCaseResults.Count(x => x.Verdict == Verdict.OK);

        question.QuestionVerdict = total == 0
            ? QuestionVerdict.Incorrect
            : passed == total
                ? QuestionVerdict.Correct
                : passed > 0
                    ? QuestionVerdict.PartiallyCorrect
                    : QuestionVerdict.Incorrect;

        
        question.ErrorMessage = request.OverallVerdict == Verdict.FailedSystem
            ? string.IsNullOrWhiteSpace(request.ErrorMessage)
                ? "Системная ошибка проверки кода"
                : request.ErrorMessage
            : null;

        var testCasesById = question.TestCases.ToDictionary(tc => tc.Id);
        foreach (var result in request.TestCaseResults)
        {
            if (!testCasesById.TryGetValue(result.InterviewTestCaseId, out var testCase))
                continue;

            testCase.ActualOutput = result.ActualOutput;
            testCase.ExecutionTimeMs = result.TimeElapsedMs;
            testCase.MemoryUsedMb = result.MemoryUsedMb;
            testCase.Verdict = result.Verdict;
            testCase.ErrorMessage = string.IsNullOrWhiteSpace(result.Error) ? null : result.Error;
        }

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
