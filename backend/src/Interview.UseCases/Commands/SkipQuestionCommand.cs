using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.Commands;

public record SkipQuestionCommand(Guid CandidateId) : IRequest<Result>;

internal class SkipQuestionCommandHandler(
    IDbContext dbContext, 
    ICurrentQuestionResolver currentQuestionResolver,
    IInterviewSessionFinalizer interviewSessionFinalizer) : IRequestHandler<SkipQuestionCommand, Result>
{
    public async Task<Result> Handle(SkipQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await currentQuestionResolver.GetCurrentQuestionAsync(request.CandidateId, cancellationToken);

        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Текущее задание не найдено"));

        var result = question.Skip();
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);
        await interviewSessionFinalizer.TryFinishIfNoActiveQuestionsAsync(question.InterviewSessionId, cancellationToken);
        return Result.Success();
    }
}