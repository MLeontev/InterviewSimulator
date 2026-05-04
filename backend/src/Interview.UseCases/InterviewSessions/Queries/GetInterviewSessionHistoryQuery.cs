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

/// <summary>
/// Элемент истории завершенных сессий собеседований
/// </summary>
public record InterviewSessionHistoryItem
{
    /// <summary>
    /// Идентификатор сессии собеседования
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Идентификатор пресета собеседования
    /// </summary>
    public Guid InterviewPresetId { get; init; }

    /// <summary>
    /// Название пресета собеседования
    /// </summary>
    public string InterviewPresetName { get; init; } = string.Empty;

    /// <summary>
    /// Состояние сессии собеседования
    /// </summary>
    public InterviewStatus Status { get; init; }

    /// <summary>
    /// Итоговый вердикт по сессии собеседования
    /// </summary>
    public SessionVerdict SessionVerdict { get; init; }

    /// <summary>
    /// Дата и время начала сессии
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Плановая дата и время завершения сессии
    /// </summary>
    public DateTime PlannedEndAt { get; init; }

    /// <summary>
    /// Фактическая дата и время завершения сессии
    /// </summary>
    public DateTime? FinishedAt { get; init; }

    /// <summary>
    /// Общее количество заданий в сессии
    /// </summary>
    public int TotalQuestions { get; init; }

    /// <summary>
    /// Количество заданий, выполнение которых было начато, завершено или пропущено
    /// </summary>
    public int CompletedQuestions { get; init; }
}
