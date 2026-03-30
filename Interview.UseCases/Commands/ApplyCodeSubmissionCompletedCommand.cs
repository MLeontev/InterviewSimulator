using CodeExecution.IntegrationEvents;
using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Verdict = Interview.Domain.Verdict;

namespace Interview.UseCases.Commands;

public record ApplyCodeSubmissionCompletedCommand(CodeSubmissionCompleted Event) : IRequest<Result>;

internal sealed class ApplyCodeSubmissionCompletedCommandHandler(IDbContext dbContext)
    : IRequestHandler<ApplyCodeSubmissionCompletedCommand, Result>
{
    public async Task<Result> Handle(ApplyCodeSubmissionCompletedCommand request, CancellationToken ct)
    {
        var message = request.Event;

        var question = await dbContext.InterviewQuestions
            .Include(q => q.TestCases)
            .FirstOrDefaultAsync(q => q.Id == message.SubmissionId, ct);

        if (question == null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Вопрос для результата проверки не найден"));

        question.OverallVerdict = MapVerdict(message.OverallVerdict);
        question.EvaluatedAt = DateTime.UtcNow;
        question.Status = QuestionStatus.EvaluatedCode;

        var testCasesById = question.TestCases.ToDictionary(tc => tc.Id);
        foreach (var result in message.TestCaseResults)
        {
            if (!testCasesById.TryGetValue(result.TestCaseId, out var testCase))
                continue;

            testCase.ActualOutput = result.ActualOutput;
            testCase.ExecutionTimeMs = result.TimeElapsed;
            testCase.MemoryUsedMb = result.MemoryUsage;
            testCase.Verdict = MapVerdict(result.Verdict);
        }

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static Verdict MapVerdict(CodeExecution.IntegrationEvents.Verdict verdict) =>
        verdict switch
        {
            CodeExecution.IntegrationEvents.Verdict.OK => Verdict.OK,
            CodeExecution.IntegrationEvents.Verdict.CE => Verdict.CE,
            CodeExecution.IntegrationEvents.Verdict.RE => Verdict.RE,
            CodeExecution.IntegrationEvents.Verdict.TLE => Verdict.TLE,
            CodeExecution.IntegrationEvents.Verdict.MLE => Verdict.MLE,
            CodeExecution.IntegrationEvents.Verdict.WA => Verdict.WA,
            _ => Verdict.FailedSystem
        };
}
