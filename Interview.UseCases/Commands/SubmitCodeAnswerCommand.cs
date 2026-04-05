using FluentValidation;
using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.IntegrationEvents;
using Interview.UseCases.Services;
using MassTransit;
using MediatR;

namespace Interview.UseCases.Commands;

public record SubmitCodeAnswerCommand(Guid CandidateId, string Code) : IRequest<Result>;

internal class SubmitCodeAnswerCommandValidator : AbstractValidator<SubmitCodeAnswerCommand>
{
    public SubmitCodeAnswerCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код не может быть пустым");
    }
}

internal sealed class SubmitCodeAnswerCommandHandler(
    IDbContext dbContext, 
    IBus bus,
    ICurrentQuestionResolver currentQuestionResolver) : IRequestHandler<SubmitCodeAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitCodeAnswerCommand request, CancellationToken ct)
    {
        var question = await currentQuestionResolver.GetCurrentQuestionAsync(request.CandidateId, ct);

        if (question == null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));

        if (question.Type != QuestionType.Coding)
            return Result.Failure(Error.Business("QUESTION_NOT_CODING", "Задание не является задачей на написание кода"));

        if (question.InterviewSession.PlannedEndAt <= DateTime.UtcNow)
            return Result.Failure(Error.Business("SESSION_EXPIRED", "Время сессии истекло"));

        if (string.IsNullOrWhiteSpace(question.ProgrammingLanguageCode))
            return Result.Failure(Error.Business("LANGUAGE_NOT_SET", "Для задания не задан язык программирования"));

        var submissionId = Guid.NewGuid();
        
        question.Answer = request.Code;
        question.Status = QuestionStatus.EvaluatingCode;
        question.SubmittedAt = DateTime.UtcNow;
        question.QuestionVerdict = QuestionVerdict.None;
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
