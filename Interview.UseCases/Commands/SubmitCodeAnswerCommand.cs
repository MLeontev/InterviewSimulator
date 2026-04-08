using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.Commands;

public record SubmitCodeAnswerCommand(Guid CandidateId) : IRequest<Result>;

internal sealed class SubmitCodeAnswerCommandHandler(
    IDbContext dbContext,
    ICurrentQuestionResolver currentQuestionResolver) : IRequestHandler<SubmitCodeAnswerCommand, Result>
{
    public async Task<Result> Handle(SubmitCodeAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await currentQuestionResolver.GetCurrentQuestionAsync(request.CandidateId, cancellationToken);

        if (question == null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));

        if (question.Type != QuestionType.Coding)
            return Result.Failure(Error.Business("QUESTION_INVALID_TYPE", "Задание не является задачей на написание кода"));
        
        if (question.InterviewSession.PlannedEndAt <= DateTime.UtcNow)
            return Result.Failure(Error.Business("SESSION_EXPIRED", "Время сессии истекло"));
        
        if (question.Status != QuestionStatus.EvaluatedCode)
            return Result.Failure(Error.Business("CODE_NOT_EVALUATED", "Сначала дождитесь результата проверки кода на тестах"));

        question.Status = QuestionStatus.Submitted;
        question.SubmittedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}