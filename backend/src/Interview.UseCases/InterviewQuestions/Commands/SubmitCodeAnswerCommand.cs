using Framework.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.InterviewQuestions.Commands;

public record SubmitCodeAnswerCommand(Guid CandidateId) : IRequest<Result>;

internal sealed class SubmitCodeAnswerCommandHandler(
    IDbContext dbContext,
    ICurrentQuestionResolver currentQuestionResolver,
    IInterviewSessionFinalizer interviewSessionFinalizer) : IRequestHandler<SubmitCodeAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitCodeAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await currentQuestionResolver.GetCurrentQuestionAsync(request.CandidateId, cancellationToken);

        if (question == null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));

        if (question.InterviewSession.PlannedEndAt <= DateTime.UtcNow)
            return Result.Failure(Error.Business("SESSION_EXPIRED", "Время сессии истекло"));
        
        var result = question.SubmitCode(DateTime.UtcNow);
        if (result.IsFailure) return result;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await interviewSessionFinalizer.TryFinishIfNoActiveQuestionsAsync(question.InterviewSessionId, cancellationToken);
        return Result.Success();
    }
}