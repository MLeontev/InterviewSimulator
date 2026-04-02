using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record SubmitCodeAnswerCommand(Guid QuestionId, string Code) : IRequest<Result>;

internal sealed class SubmitCodeAnswerCommandHandler(IDbContext dbContext, IBus bus) : IRequestHandler<SubmitCodeAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitCodeAnswerCommand request, CancellationToken ct)
    {
        var question = await dbContext.InterviewQuestions
            .Include(q => q.InterviewSession)
            .Include(q => q.TestCases)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct);

        if (question == null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));

        if (question.Type != QuestionType.Coding)
            return Result.Failure(Error.Business("QUESTION_NOT_CODING", "Задание не является задачей на написание кода"));

        if (question.InterviewSession.Status != InterviewStatus.InProgress)
            return Result.Failure(Error.Business("SESSION_NOT_ACTIVE", "Сессия уже завершена"));

        if (string.IsNullOrWhiteSpace(question.ProgrammingLanguageCode))
            return Result.Failure(Error.Business("LANGUAGE_NOT_SET", "Для задания не задан язык программирования"));

        var submissionId = Guid.NewGuid();
        
        question.Answer = request.Code;
        question.Status = QuestionStatus.EvaluatingCode;
        question.SubmittedAt = DateTime.UtcNow;
        question.LastSubmissionId = submissionId;

        foreach (var testCase in question.TestCases)
        {
            testCase.ActualOutput = null;
            testCase.ExecutionTimeMs = null;
            testCase.MemoryUsedMb = null;
            testCase.Verdict = Verdict.None;
        }

        var eventPayload = new CodeSubmissionCreated(
            SubmissionId: submissionId,
            InterviewQuestionId: question.Id,
            Code: request.Code,
            LanguageCode: question.ProgrammingLanguageCode,
            TestCases: question.TestCases
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new CodeSubmissionCreatedTestCase(
                    InterviewTestCaseId: tc.Id,
                    OrderIndex: tc.OrderIndex,
                    Input: tc.Input,
                    ExpectedOutput: tc.ExpectedOutput))
                .ToArray(),
            TimeLimitMs: question.TimeLimitMs,
            MemoryLimitMb: question.MemoryLimitMb);

        await dbContext.SaveChangesAsync(ct);
        await bus.Publish(eventPayload, ct);

        return Result.Success();
    }
}
