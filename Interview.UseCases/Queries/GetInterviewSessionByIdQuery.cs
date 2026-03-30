using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Queries;

public record GetInterviewSessionByIdQuery(Guid SessionId) : IRequest<Result<InterviewSessionDto>>;

internal class GetInterviewSessionByIdQueryHandler(IDbContext dbContext) : IRequestHandler<GetInterviewSessionByIdQuery, Result<InterviewSessionDto>>
{
    public async Task<Result<InterviewSessionDto>> Handle(GetInterviewSessionByIdQuery request, CancellationToken ct)
    {
        var sessionDto = await dbContext.InterviewSessions
            .Where(s => s.Id == request.SessionId)
            .Select(s => new InterviewSessionDto
            {
                Id = s.Id,
                InterviewPresetName = s.InterviewPresetName,
                StartTime = s.StartedAt,
                EndTime = s.FinishedAt ?? s.PlannedEndAt,
                Status = s.Status.ToString(),
                TotalQuestions = s.Questions.Count,
                AnsweredQuestions = s.Questions.Count(q => q.Status >= QuestionStatus.Submitted),
                QuestionIds = s.Questions.OrderBy(q => q.OrderIndex).Select(q => q.Id).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (sessionDto == null)
        {
            return Result.Failure<InterviewSessionDto>(
                Error.NotFound("SESSION_NOT_FOUND", "Сессия интервью не найдена"));
            
        }

        return Result.Success(sessionDto);
    }
}

public record InterviewSessionDto
{
    public Guid Id { get; init; }
    public string InterviewPresetName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalQuestions { get; init; }
    public int AnsweredQuestions { get; init; }
    public IReadOnlyList<Guid> QuestionIds { get; init; } = [];
}
