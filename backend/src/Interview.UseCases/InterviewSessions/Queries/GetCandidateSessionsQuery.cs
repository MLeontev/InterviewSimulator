using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.InterviewSessions.Queries;

public record GetCandidateSessionsQuery(Guid CandidateId) : IRequest<Result<IReadOnlyList<CandidateSessionListItemDto>>>;

internal class GetCandidateSessionsQueryHandler(IDbContext dbContext) : IRequestHandler<GetCandidateSessionsQuery, Result<IReadOnlyList<CandidateSessionListItemDto>>>
{
    public async Task<Result<IReadOnlyList<CandidateSessionListItemDto>>> Handle(GetCandidateSessionsQuery request, CancellationToken ct)
    {
        var sessions = await dbContext.InterviewSessions
            .Where(s => s.CandidateId == request.CandidateId)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new CandidateSessionListItemDto
            {
                Id = s.Id,
                InterviewPresetName = s.InterviewPresetName,
                StartTime = s.StartedAt,
                EndTime = s.FinishedAt ?? s.PlannedEndAt,
                Status = s.Status.ToString(),
                SessionVerdict = s.SessionVerdict,
                TotalQuestions = s.Questions.Count,
                AnsweredQuestions = s.Questions.Count(q =>
                    InterviewQuestionStatusRules.Answered.Contains(q.Status))
            })
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<CandidateSessionListItemDto>>(sessions);
    }
}

public record CandidateSessionListItemDto
{
    public Guid Id { get; init; }
    public string InterviewPresetName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public SessionVerdict SessionVerdict { get; init; }
    public int TotalQuestions { get; init; }
    public int AnsweredQuestions { get; init; }
}
