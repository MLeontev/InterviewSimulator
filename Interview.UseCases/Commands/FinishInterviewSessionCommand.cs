using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record FinishInterviewSessionCommand(Guid CandidateId) : IRequest<Result>;

internal class FinishInterviewSessionCommandHandler(IDbContext dbContext) : IRequestHandler<FinishInterviewSessionCommand, Result>
{
    public async Task<Result> Handle(FinishInterviewSessionCommand request, CancellationToken ct)
    {
        var session = await dbContext.InterviewSessions
            .Where(s => s.CandidateId == request.CandidateId
                        && s.Status == InterviewStatus.InProgress
                        && s.PlannedEndAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (session == null)
            return Result.Failure(Error.NotFound("SESSION_NOT_FOUND", "Текущая сессия интервью не найдена"));
        
        session.Status = InterviewStatus.Finished;
        session.FinishedAt = DateTime.UtcNow;
        
        foreach (var q in session.Questions.Where(x => x.Status == QuestionStatus.InProgress))
            q.Status = QuestionStatus.Skipped;

        await dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
