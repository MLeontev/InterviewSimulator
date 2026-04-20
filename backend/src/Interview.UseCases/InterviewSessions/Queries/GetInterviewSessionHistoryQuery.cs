using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.InterviewSessions.Queries;

public record GetInterviewSessionHistoryQuery(
    Guid CandidateId) : IRequest<Result<IReadOnlyList<InterviewSessionHistoryItem>>>;

internal class GetInterviewSessionHistoryQueryHandler(IDbContext dbContext)
    : IRequestHandler<GetInterviewSessionHistoryQuery, Result<IReadOnlyList<InterviewSessionHistoryItem>>>
{
    public async Task<Result<IReadOnlyList<InterviewSessionHistoryItem>>> Handle(
        GetInterviewSessionHistoryQuery request, 
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.InterviewSessions
            .AsNoTracking()
            .Where(x => x.CandidateId == request.CandidateId && x.Status != InterviewStatus.InProgress)
            .OrderByDescending(s => s.FinishedAt ?? s.PlannedEndAt)
            .Select(x => new InterviewSessionHistoryItem
            {
                SessionId = x.Id,
                InterviewPresetId = x.InterviewPresetId,
                InterviewPresetName = x.InterviewPresetName,
                Status = x.Status,
                SessionVerdict = x.SessionVerdict,
                StartedAt = x.StartedAt,
                PlannedEndAt = x.PlannedEndAt,
                FinishedAt = x.FinishedAt,
                TotalQuestions = x.Questions.Count,
                CompletedQuestions = x.Questions.Count(q =>
                    q.Status != QuestionStatus.NotStarted &&
                    q.Status != QuestionStatus.InProgress)
            })
            .ToListAsync(cancellationToken);
        
        return Result.Success<IReadOnlyList<InterviewSessionHistoryItem>>(sessions);
    }
}

public record InterviewSessionHistoryItem
{
    public Guid SessionId { get; init; }
    public Guid InterviewPresetId { get; init; }
    public string InterviewPresetName { get; init; } = string.Empty;

    public InterviewStatus Status { get; init; }
    public SessionVerdict SessionVerdict { get; init; }

    public DateTime StartedAt { get; init; }
    public DateTime PlannedEndAt { get; init; }
    public DateTime? FinishedAt { get; init; }

    public int TotalQuestions { get; init; }
    public int CompletedQuestions { get; init; }
}