using Framework.Domain;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Verdict = Interview.Domain.Enums.Verdict;

namespace Interview.UseCases.InterviewSessions.Queries;

public record GetInterviewSessionReportQuery(Guid CandidateId, Guid SessionId) : IRequest<Result<InterviewSessionReportDto>>;

internal class GetInterviewSessionReportQueryHandler(IDbContext dbContext) : IRequestHandler<GetInterviewSessionReportQuery, Result<InterviewSessionReportDto>>
{
    public async Task<Result<InterviewSessionReportDto>> Handle(GetInterviewSessionReportQuery request, CancellationToken cancellationToken)
    {
        var session = await dbContext.InterviewSessions
            .AsNoTracking()
            .Where(s => s.Id == request.SessionId && s.CandidateId == request.CandidateId)
            .Include(s => s.Questions)
            .ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (session is null)
            return Result.Failure<InterviewSessionReportDto>(
                Error.NotFound("SESSION_NOT_FOUND", "Сессия не найдена"));

        if (session.Status is not (InterviewStatus.Evaluated or InterviewStatus.AiEvaluationFailed))
            return Result.Failure<InterviewSessionReportDto>(
                Error.Business("SESSION_NOT_FINISHED", "Сессия еще не оценена"));
        
        var questionItems = session.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(MapQuestion)
            .ToList();

        var questionScores = session.Questions
            .Select(InterviewQuestionScoreResolver.Resolve)
            .ToList();

        var averageQuestionScore = InterviewSessionScoringPolicy.CalculateOverallScore(questionScores);

        var totalQuestions = session.Questions.Count;
        var answeredQuestions = session.Questions.Count(q => q.Status == QuestionStatus.EvaluatedAi);
        
        var (sessionSummary, sessionStrengths, sessionWeaknesses, sessionRecommendations) = ParseSessionAiFeedback(session.AiFeedbackJson);
        
        var report = new InterviewSessionReportDto
        {
            SessionId = session.Id,
            CandidateId = session.CandidateId,
            InterviewPresetId = session.InterviewPresetId,
            InterviewPresetName = session.InterviewPresetName,

            Status = session.Status,
            SessionVerdict = session.SessionVerdict,

            StartedAt = session.StartedAt,
            PlannedEndAt = session.PlannedEndAt,
            FinishedAt = session.FinishedAt,
            TotalQuestions = totalQuestions,
            AnsweredQuestions = answeredQuestions,

            AverageQuestionAiScore = averageQuestionScore,

            SessionSummary = sessionSummary,
            SessionStrengths = sessionStrengths,
            SessionWeaknesses = sessionWeaknesses,
            SessionRecommendations = sessionRecommendations,

            Questions = questionItems
        };

        return Result.Success(report);
    }

    private InterviewSessionReportQuestionDto MapQuestion(InterviewQuestion q)
    {
        var (score, feedback) = ParseQuestionAiFeedback(q.AiFeedbackJson);
        int? totalTests = null;
        int? passedTests = null;

        if (q.Type == QuestionType.Coding)
        {
            totalTests = q.TestCases.Count;
            passedTests = q.TestCases.Count(tc => tc.Verdict == Verdict.OK);
        }

        return new InterviewSessionReportQuestionDto
        {
            QuestionId = q.Id,
            OrderIndex = q.OrderIndex,

            Type = q.Type,
            Status = q.Status,
            QuestionVerdict = q.QuestionVerdict,
            OverallVerdict = q.OverallVerdict,

            Title = string.IsNullOrWhiteSpace(q.Title) ? "Без названия" : q.Title,
            Text = q.Text,
            Answer = q.Answer,
            ProgrammingLanguageCode = q.ProgrammingLanguageCode,

            StartedAt = q.StartedAt,
            SubmittedAt = q.SubmittedAt,
            EvaluatedAt = q.EvaluatedAt,

            TimeLimitMs = q.TimeLimitMs,
            MemoryLimitMb = q.MemoryLimitMb,
            ErrorMessage = q.ErrorMessage,

            AiScore = score,
            AiFeedback = feedback,
            PassedTests = passedTests,
            TotalTests = totalTests
        };
    }
    
    private static (int? Score, string? Feedback) ParseQuestionAiFeedback(string? rawJson)
    {
        if (!AiFeedbackJsonParser.TryParseQuestion(rawJson, out var score, out var feedback))
            return (null, null);

        return (score, feedback);
    }

    private static (
        string? Summary, 
        IReadOnlyList<string> Strengths, 
        IReadOnlyList<string> Weaknesses, 
        IReadOnlyList<string> Recommendations) ParseSessionAiFeedback(string? rawJson)
    {
        if (!AiFeedbackJsonParser.TryParseSession(rawJson, out var summary, out var strengths, out var weaknesses, out var recommendations))
            return (null, [], [], []);

        return (summary, strengths, weaknesses, recommendations);
    }
}

/// <summary>
/// Отчет по завершенной сессии собеседования
/// </summary>
public record InterviewSessionReportDto
{
    /// <summary>
    /// Идентификатор сессии собеседования
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Идентификатор кандидата
    /// </summary>
    public Guid CandidateId { get; init; }

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
    /// Количество заданий, оцененных ИИ
    /// </summary>
    public int AnsweredQuestions { get; init; }

    /// <summary>
    /// Средний балл по заданиям сессии
    /// </summary>
    public double AverageQuestionAiScore { get; init; }

    /// <summary>
    /// Общий вывод по результатам сессии
    /// </summary>
    public string? SessionSummary { get; init; }

    /// <summary>
    /// Сильные стороны подготовки кандидата
    /// </summary>
    public IReadOnlyList<string> SessionStrengths { get; init; } = [];

    /// <summary>
    /// Слабые стороны подготовки кандидата
    /// </summary>
    public IReadOnlyList<string> SessionWeaknesses { get; init; } = [];

    /// <summary>
    /// Рекомендации по дальнейшей подготовке
    /// </summary>
    public IReadOnlyList<string> SessionRecommendations { get; init; } = [];

    /// <summary>
    /// Результаты выполнения отдельных заданий
    /// </summary>
    public IReadOnlyList<InterviewSessionReportQuestionDto> Questions { get; init; } = [];
}

/// <summary>
/// Результат выполнения задания в отчете по сессии
/// </summary>
public record InterviewSessionReportQuestionDto
{
    /// <summary>
    /// Идентификатор задания в рамках сессии
    /// </summary>
    public Guid QuestionId { get; init; }

    /// <summary>
    /// Порядковый номер задания в сессии
    /// </summary>
    public int OrderIndex { get; init; }

    /// <summary>
    /// Тип задания
    /// </summary>
    public QuestionType Type { get; init; }

    /// <summary>
    /// Состояние выполнения задания
    /// </summary>
    public QuestionStatus Status { get; init; }

    /// <summary>
    /// Вердикт по заданию
    /// </summary>
    public QuestionVerdict QuestionVerdict { get; init; }

    /// <summary>
    /// Общий вердикт проверки кода
    /// </summary>
    public Verdict OverallVerdict { get; init; }

    /// <summary>
    /// Название задания
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Формулировка задания
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Ответ кандидата или отправленный программный код
    /// </summary>
    public string? Answer { get; init; }

    /// <summary>
    /// Код языка программирования для задачи на написание кода
    /// </summary>
    public string? ProgrammingLanguageCode { get; init; }

    /// <summary>
    /// Дата и время начала выполнения задания
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Дата и время отправки ответа или решения
    /// </summary>
    public DateTime? SubmittedAt { get; init; }

    /// <summary>
    /// Дата и время получения оценки задания
    /// </summary>
    public DateTime? EvaluatedAt { get; init; }

    /// <summary>
    /// Ограничение времени выполнения кода в миллисекундах
    /// </summary>
    public int? TimeLimitMs { get; init; }

    /// <summary>
    /// Ограничение памяти выполнения кода в мегабайтах
    /// </summary>
    public int? MemoryLimitMb { get; init; }

    /// <summary>
    /// Сообщение об ошибке проверки кода
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Балл AI-оценки за задание
    /// </summary>
    public int? AiScore { get; init; }

    /// <summary>
    /// Текстовая обратная связь по заданию
    /// </summary>
    public string? AiFeedback { get; init; }

    /// <summary>
    /// Количество пройденных тестов для задачи на написание кода
    /// </summary>
    public int? PassedTests { get; init; }

    /// <summary>
    /// Общее количество тестов для задачи на написание кода
    /// </summary>
    public int? TotalTests { get; init; }
}
