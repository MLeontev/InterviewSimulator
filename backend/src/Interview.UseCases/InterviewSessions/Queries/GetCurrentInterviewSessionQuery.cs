using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.InterviewSessions.Queries;

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
                    InterviewQuestionStatusRules.Answered.Contains(q.Status))
            })
            .SingleOrDefaultAsync(ct);

        if (session is null)
            return Result.Failure<CurrentInterviewSession>(
                Error.NotFound("SESSION_NOT_FOUND", "Текущая сессия интервью не найдена"));

        return Result.Success(session);
    }
}

/// <summary>
/// Текущая активная сессия собеседования
/// </summary>
public record CurrentInterviewSession
{
    /// <summary>
    /// Идентификатор сессии собеседования
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Состояние сессии собеседования
    /// </summary>
    public InterviewStatus Status { get; init; }

    /// <summary>
    /// Дата и время начала сессии
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Плановая дата и время завершения сессии
    /// </summary>
    public DateTime PlannedEndAt { get; init; }

    /// <summary>
    /// Общее количество заданий в сессии
    /// </summary>
    public int TotalQuestions { get; init; }

    /// <summary>
    /// Количество заданий, по которым кандидат уже отправил ответ или решение
    /// </summary>
    public int AnsweredQuestions { get; init; }
}
