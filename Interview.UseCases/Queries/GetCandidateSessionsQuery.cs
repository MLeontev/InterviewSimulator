using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Queries;

public record GetCandidateSessionsQuery(Guid CandidateId) : IRequest<Result<IReadOnlyList<CandidateSessionListItemDto>>>;

internal class GetCandidateSessionsQueryHandler(IDbContext dbContext) : IRequestHandler<GetCandidateSessionsQuery, Result<IReadOnlyList<CandidateSessionListItemDto>>>
{
    public async Task<Result<IReadOnlyList<CandidateSessionListItemDto>>> Handle(GetCandidateSessionsQuery request, CancellationToken ct)
    {
        var sessions = await dbContext.InterviewSessions
            .Where(s => s.CandidateId == request.CandidateId)
            .OrderByDescending(s => s.StartTime)
            .Select(s => new CandidateSessionListItemDto
            {
                Id = s.Id,
                InterviewPresetName = s.InterviewPresetName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Status = s.Status.ToString(),
                TotalQuestions = s.Questions.Count,
                AnsweredQuestions = s.Questions.Count(q => q.Status >= QuestionStatus.Submitted)
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
    public int TotalQuestions { get; init; }
    public int AnsweredQuestions { get; init; }
}