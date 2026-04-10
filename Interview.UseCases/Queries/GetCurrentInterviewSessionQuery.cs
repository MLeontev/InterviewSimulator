using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Queries;

public record GetCurrentInterviewSessionQuery(Guid CandidateId) : IRequest<Result<CurrentInterviewSession>>;

internal class GetCurrentInterviewSessionQueryHandler(IDbContext dbContext) : IRequestHandler<GetCurrentInterviewSessionQuery, Result<CurrentInterviewSession>>
{
    public async Task<Result<CurrentInterviewSession>> Handle(GetCurrentInterviewSessionQuery request, CancellationToken ct)
    {
        var session = await dbContext.InterviewSessions
            .AsNoTracking()
            .Where(s => s.CandidateId == request.CandidateId && s.Status == InterviewStatus.InProgress)
            .Select(s => new CurrentInterviewSession
            {
                SessionId = s.Id,
                Status = s.Status,
                StartedAt = s.StartedAt,
                PlannedEndAt = s.PlannedEndAt,
                TotalQuestions = s.Questions.Count,
                AnsweredQuestions = s.Questions.Count(q =>
                    q.Status == QuestionStatus.Submitted ||
                    q.Status == QuestionStatus.EvaluatingAi ||
                    q.Status == QuestionStatus.EvaluatedAi)
            })
            .SingleOrDefaultAsync(ct);

        if (session is null)
            return Result.Failure<CurrentInterviewSession>(
                Error.NotFound("SESSION_NOT_FOUND", "Текущая сессия интервью не найдена"));

        return Result.Success(session);
    }
}

public record CurrentInterviewSession
{
    public Guid SessionId { get; init; }
    public InterviewStatus Status { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime PlannedEndAt { get; init; }
    public int TotalQuestions { get; init; }
    public int AnsweredQuestions { get; init; }
}
