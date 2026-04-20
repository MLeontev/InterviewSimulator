using FluentValidation;
using Framework.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.InterviewQuestions.Commands;

public record SubmitTheoryAnswerCommand(Guid CandidateId, string Answer) : IRequest<Result>;

internal class SubmitTheoryAnswerCommandValidator : AbstractValidator<SubmitTheoryAnswerCommand>
{
    public SubmitTheoryAnswerCommandValidator()
    {
        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("Ответ не может быть пустым");
    }
}

internal class SubmitTheoryAnswerCommandHandler(
    IDbContext dbContext,
    ICurrentQuestionResolver currentQuestionResolver,
    IInterviewSessionFinalizer interviewSessionFinalizer) : IRequestHandler<SubmitTheoryAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitTheoryAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await currentQuestionResolver.GetCurrentQuestionAsync(request.CandidateId, cancellationToken);
        
        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));
        
        if (question.InterviewSession.PlannedEndAt <= DateTime.UtcNow)
            return Result.Failure(Error.Business("SESSION_EXPIRED", "Время сессии истекло"));

        var result = question.SubmitTheoryAnswer(request.Answer, DateTime.UtcNow);
        if (result.IsFailure) return result;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await interviewSessionFinalizer.TryFinishIfNoActiveQuestionsAsync(question.InterviewSessionId, cancellationToken);
        return Result.Success();
    }
}