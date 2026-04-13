using FluentValidation;
using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.IntegrationEvents;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.Commands;

public record SubmitDraftCodeAnswerCommand(Guid CandidateId, string Code) : IRequest<Result>;

internal class SubmitDraftCodeAnswerCommandValidator : AbstractValidator<SubmitDraftCodeAnswerCommand>
{
    public SubmitDraftCodeAnswerCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код не может быть пустым");
    }
}

internal sealed class SubmitDraftCodeAnswerCommandHandler(
    IDbContext dbContext, 
    ICurrentQuestionResolver currentQuestionResolver) : IRequestHandler<SubmitDraftCodeAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitDraftCodeAnswerCommand request, CancellationToken ct)
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

        if (question.Status == QuestionStatus.EvaluatingCode)
            return Result.Failure(Error.Business(
                "CODE_CHECK_IN_PROGRESS",
                "Проверка кода уже выполняется. Дождитесь результата текущего запуска"));

        if (question.Status is not (QuestionStatus.InProgress or QuestionStatus.EvaluatedCode))
            return Result.Failure(Error.Business(
                "QUESTION_NOT_READY_FOR_DRAFT_SUBMIT",
                "Черновую отправку можно выполнить только для начатого задания или после предыдущей проверки"));

        var submissionId = Guid.NewGuid();
        
        question.Answer = request.Code;
        question.Status = QuestionStatus.EvaluatingCode;
        question.SubmittedAt = DateTime.UtcNow;
        question.EvaluatedAt = null;
        question.AiFeedbackJson = null;
        question.ErrorMessage = null;
        question.QuestionVerdict = QuestionVerdict.None;
        question.OverallVerdict = Verdict.None;
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
        
        dbContext.AddOutboxMessage(eventPayload);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
