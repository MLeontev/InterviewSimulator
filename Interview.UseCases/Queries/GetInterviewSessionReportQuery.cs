using System.Text.Json;
using System.Text.Json.Serialization;
using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Verdict = Interview.Domain.Verdict;

namespace Interview.UseCases.Queries;

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

        if (session.Status != InterviewStatus.Evaluated)
            return Result.Failure<InterviewSessionReportDto>(
                Error.Business("SESSION_NOT_FINISHED", "Сессия еще не оценена"));
        
        var questionItems = session.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(MapQuestion)
            .ToList();
        
        var questionScores = questionItems
            .Where(x => x.AiScore.HasValue)
            .Select(x => (double)x.AiScore!.Value)
            .ToList();
        
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

            AverageQuestionAiScore = questionScores.Count != 0
                ? Math.Round(questionScores.Average(), 2)
                : null,

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

            TestCases = q.TestCases
                .Where(tc => !tc.IsHidden)
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new InterviewSessionReportTestCaseDto
                {
                    OrderIndex = tc.OrderIndex,
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    ActualOutput = tc.ActualOutput,
                    Verdict = tc.Verdict,
                    ExecutionTimeMs = tc.ExecutionTimeMs,
                    MemoryUsedMb = tc.MemoryUsedMb,
                    ErrorMessage = tc.ErrorMessage
                })
                .ToList()
        };
    }
    
    private (int? Score, string? Feedback) ParseQuestionAiFeedback(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) 
            return (null, null);

        try
        {
            var result = JsonSerializer.Deserialize<QuestionAiFeedback>(rawJson);
            var score = result?.Score is >= 0 and <= 10 ? result.Score : null;
            return (score, result?.Feedback);
        }
        catch
        {
            return (null, null);
        }
    }

    private (string? Summary, IReadOnlyList<string> Strengths, IReadOnlyList<string> Weaknesses, IReadOnlyList<string> Recommendations) ParseSessionAiFeedback(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) 
            return (null, [], [], []);

        try
        {
            var result = JsonSerializer.Deserialize<SessionAiFeedback>(rawJson);
            
            return (
                result?.Summary,
                result?.Strengths ?? [],
                result?.Weaknesses ?? [],
                result?.Recommendations ?? []);
        }
        catch
        {
            return (null, [], [], []);
        }
    }
    
    private record QuestionAiFeedback(
        [property: JsonPropertyName("score")] int? Score,
        [property: JsonPropertyName("feedback")] string? Feedback);

    private record SessionAiFeedback(
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("strengths")] List<string>? Strengths,
        [property: JsonPropertyName("weaknesses")] List<string>? Weaknesses,
        [property: JsonPropertyName("recommendations")] List<string>? Recommendations);
}

public record InterviewSessionReportDto
{
    public Guid SessionId { get; init; }
    public Guid CandidateId { get; init; }

    public Guid InterviewPresetId { get; init; }
    public string InterviewPresetName { get; init; } = string.Empty;

    public InterviewStatus Status { get; init; }
    public SessionVerdict SessionVerdict { get; init; }

    public DateTime StartedAt { get; init; }
    public DateTime PlannedEndAt { get; init; }
    public DateTime? FinishedAt { get; init; }

    public double? AverageQuestionAiScore { get; init; }

    public string? SessionSummary { get; init; }
    public IReadOnlyList<string> SessionStrengths { get; init; } = [];
    public IReadOnlyList<string> SessionWeaknesses { get; init; } = [];
    public IReadOnlyList<string> SessionRecommendations { get; init; } = [];

    public IReadOnlyList<InterviewSessionReportQuestionDto> Questions { get; init; } = [];
}

public record InterviewSessionReportQuestionDto
{
    public Guid QuestionId { get; init; }
    public int OrderIndex { get; init; }

    public QuestionType Type { get; init; }
    public QuestionStatus Status { get; init; }
    public QuestionVerdict QuestionVerdict { get; init; }
    public Verdict OverallVerdict { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string? Answer { get; init; }
    public string? ProgrammingLanguageCode { get; init; }

    public DateTime? StartedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? EvaluatedAt { get; init; }

    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitMb { get; init; }
    public string? ErrorMessage { get; init; }

    public int? AiScore { get; init; }
    public string? AiFeedback { get; init; }

    public IReadOnlyList<InterviewSessionReportTestCaseDto> TestCases { get; init; } = [];
}

public record InterviewSessionReportTestCaseDto
{
    public int OrderIndex { get; init; }
    public string Input { get; init; } = string.Empty;
    public string ExpectedOutput { get; init; } = string.Empty;
    public string? ActualOutput { get; init; }
    public Verdict Verdict { get; init; }
    public double? ExecutionTimeMs { get; init; }
    public double? MemoryUsedMb { get; init; }
    public string? ErrorMessage { get; init; }
}