using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.Commands;

public record StartCurrentInterviewQuestionCommand(Guid CandidateId) : IRequest<Result>;

internal class StartCurrentInterviewQuestionCommandHandler(
    IDbContext dbContext,
    ICurrentQuestionResolver currentQuestionResolver) : IRequestHandler<StartCurrentInterviewQuestionCommand, Result>
{
    public async Task<Result> Handle(StartCurrentInterviewQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await currentQuestionResolver.GetCurrentQuestionAsync(request.CandidateId, cancellationToken);

        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Текущее задание не найдено"));

        if (question.InterviewSession.PlannedEndAt <= DateTime.UtcNow)
            return Result.Failure(Error.Business("SESSION_EXPIRED", "Время сессии истекло"));

        if (question.Status == QuestionStatus.InProgress)
            return Result.Success();

        if (question.Status != QuestionStatus.NotStarted)
            return Result.Failure(Error.Business("QUESTION_CANNOT_BE_STARTED", "Это задание нельзя запустить в текущем статусе"));

        question.Status = QuestionStatus.InProgress;
        question.StartedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}