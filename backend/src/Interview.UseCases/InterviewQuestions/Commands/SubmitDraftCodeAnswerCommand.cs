using FluentValidation;
using Framework.Domain;
using Interview.Domain.Entities;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.IntegrationEvents;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.InterviewQuestions.Commands;

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

        if (question.InterviewSession.PlannedEndAt <= DateTime.UtcNow)
            return Result.Failure(Error.Business("SESSION_EXPIRED", "Время сессии истекло"));

        var nowUtc = DateTime.UtcNow;
        var submissionId = Guid.NewGuid();
        
        var result = question.SubmitDraftCode(request.Code, submissionId, nowUtc);
        if (result.IsFailure) return result;
        
        var eventPayload = new CodeSubmissionCreated(
            SubmissionId: submissionId,
            InterviewQuestionId: question.Id,
            Code: request.Code,
            LanguageCode: question.ProgrammingLanguageCode!,
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
