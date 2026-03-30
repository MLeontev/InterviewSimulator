using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record SubmitInterviewCodeAnswerCommand(Guid QuestionId, string Code) : IRequest<Result>;

internal sealed class SubmitInterviewCodeAnswerCommandHandler(
    IDbContext dbContext,
    IBus bus) : IRequestHandler<SubmitInterviewCodeAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitInterviewCodeAnswerCommand request, CancellationToken ct)
    {
        var question = await dbContext.InterviewQuestions
            .Include(q => q.InterviewSession)
            .Include(q => q.TestCases)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct);

        if (question == null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Вопрос не найден"));

        if (question.Type != QuestionType.Coding)
            return Result.Failure(Error.Business("QUESTION_NOT_CODING", "Вопрос не является задачей на код"));

        if (question.InterviewSession.Status != InterviewStatus.InProgress)
            return Result.Failure(Error.Business("SESSION_NOT_ACTIVE", "Сессия уже завершена"));

        if (string.IsNullOrWhiteSpace(question.ProgrammingLanguageCode))
            return Result.Failure(Error.Business("LANGUAGE_NOT_SET", "Для вопроса не задан язык программирования"));

        question.Answer = request.Code;
        question.Status = QuestionStatus.EvaluatingCode;
        question.SubmittedAt = DateTime.UtcNow;

        foreach (var testCase in question.TestCases)
        {
            testCase.ActualOutput = null;
            testCase.ExecutionTimeMs = null;
            testCase.MemoryUsedMb = null;
            testCase.Verdict = Verdict.None;
        }

        var eventPayload = new CodeSubmissionCreated(
            SubmissionId: question.Id,
            Code: request.Code,
            Language: question.ProgrammingLanguageCode,
            TestCases: question.TestCases
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new Interview.IntegrationEvents.TestCaseDto(
                    TestCaseId: tc.Id,
                    Order: tc.OrderIndex,
                    Input: tc.Input,
                    ExpectedOutput: tc.ExpectedOutput))
                .ToArray(),
            MaxTimeSeconds: question.TimeLimitMs.HasValue
                ? Math.Max(1, (int)Math.Ceiling(question.TimeLimitMs.Value / 1000.0))
                : null,
            MaxMemoryMb: question.MemoryLimitMb);

        await dbContext.SaveChangesAsync(ct);
        await bus.Publish(eventPayload, ct);

        return Result.Success();
    }
}
