using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.InterviewSessions.Commands;

public record FinishInterviewSessionCommand(Guid CandidateId) : IRequest<Result>;

internal class FinishInterviewSessionCommandHandler(IDbContext dbContext) : IRequestHandler<FinishInterviewSessionCommand, Result>
{
    public async Task<Result> Handle(FinishInterviewSessionCommand request, CancellationToken ct)
    {
        var session = await dbContext.InterviewSessions
            .Include(s => s.Questions)
            .Where(s => s.CandidateId == request.CandidateId
                        && s.Status == InterviewStatus.InProgress)
            .FirstOrDefaultAsync(ct);

        if (session == null)
            return Result.Failure(Error.NotFound("SESSION_NOT_FOUND", "Текущая сессия интервью не найдена"));

        var finishResult = session.Finish(DateTime.UtcNow);
        if (finishResult.IsFailure)
            return Result.Failure(finishResult.Error);

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
