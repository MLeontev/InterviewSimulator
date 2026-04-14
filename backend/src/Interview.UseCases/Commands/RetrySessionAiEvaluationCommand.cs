using Framework.Domain;
using Interview.Domain.Entities;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record RetrySessionAiEvaluationCommand(Guid CandidateId, Guid SessionId) : IRequest<Result>;

internal class RetrySessionAiEvaluationCommandHandler(
    IDbContext dbContext) : IRequestHandler<RetrySessionAiEvaluationCommand, Result>
{
    public async Task<Result> Handle(RetrySessionAiEvaluationCommand request, CancellationToken cancellationToken)
    {
        var session = await dbContext.InterviewSessions
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x =>
                x.Id == request.SessionId &&
                x.CandidateId == request.CandidateId, cancellationToken);
        
        if (session is null)
            return Result.Failure(Error.NotFound("SESSION_NOT_FOUND", "Сессия не найдена"));

        var retryResult = session.ResetForAiRetry(DateTime.UtcNow);
        if (retryResult.IsFailure)
            return Result.Failure(retryResult.Error);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
