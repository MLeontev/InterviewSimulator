using Framework.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.InterviewSession.Commands;

public record DeleteInterviewSessionCommand(Guid SessionId) : IRequest<Result>;

internal class DeleteInterviewSessionCommandHandler(IDbContext dbContext) : IRequestHandler<DeleteInterviewSessionCommand, Result>
{
    public async Task<Result> Handle(DeleteInterviewSessionCommand request, CancellationToken ct)
    {
        var session = await dbContext.InterviewSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);

        if (session == null)
            return Result.Failure(Error.NotFound("SESSION_NOT_FOUND", "Сессия интервью не найдена"));

        dbContext.InterviewSessions.Remove(session);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
