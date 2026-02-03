using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record FinishInterviewSessionCommand(Guid SessionId) : IRequest<Result>;

internal class FinishInterviewSessionCommandHandler(IDbContext dbContext) : IRequestHandler<FinishInterviewSessionCommand, Result>
{
    public async Task<Result> Handle(FinishInterviewSessionCommand request, CancellationToken ct)
    {
        var session = await dbContext.InterviewSessions
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);

        if (session == null)
            return Result.Failure(Error.NotFound("SESSION_NOT_FOUND", "Сессия интервью не найдена"));
        
        if (session.Status != InterviewStatus.InProgress)
            return Result.Failure(Error.Business("SESSION_NOT_ACTIVE", "Сессия уже завершена или отменена"));
        
        session.Status = InterviewStatus.Finished;
        session.EndTime = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}