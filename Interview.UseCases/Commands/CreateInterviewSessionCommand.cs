using Framework.Domain;
using MediatR;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using QuestionBank.InternalApi;
using QuestionType = Interview.Domain.QuestionType;

namespace Interview.UseCases.Commands;

public record CreateInterviewSessionCommand(Guid CandidateId, Guid InterviewPresetId) : IRequest<Result<Guid>>;

internal class CreateInterviewSessionCommandHandler(
    IDbContext dbContext,
    IQuestionBankApi questionBankApi) : IRequestHandler<CreateInterviewSessionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInterviewSessionCommand request, CancellationToken ct)
    {
        const int totalQuestions = 5;
        
        var hasActiveSession = await dbContext.InterviewSessions
            .AnyAsync(s => s.CandidateId == request.CandidateId 
                           && s.Status == InterviewStatus.InProgress, ct);

        if (hasActiveSession)
        {
            return Result.Failure<Guid>(Error.Business("ACTIVE_SESSION_EXISTS", "У кандидата уже есть активная сессия интервью"));
        }

        var presetInfo = await questionBankApi.GetPresetAsync(request.InterviewPresetId);
        if (presetInfo == null)
        {
            return Result.Failure<Guid>(Error.NotFound("PRESET_NOT_FOUND", "Пресет интервью не найден"));
        }

        var questions = await questionBankApi.GetQuestionsAsync(
            request.InterviewPresetId,
            totalQuestions);

        if (questions.Count == 0)
        {
            return Result.Failure<Guid>(Error.Business("NO_QUESTIONS", "Не удалось получить вопросы для сессии интервью"));
        }

        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var session = new InterviewSession
        {
            Id = sessionId,
            CandidateId = request.CandidateId,
            InterviewPresetName = presetInfo.Name,
            StartTime = now,
            EndTime = now.AddHours(1),
            Status = InterviewStatus.InProgress,
            Questions = []
        };

        var interviewQuestions = new List<InterviewQuestion>();
        foreach (var (question, index) in questions.Select((q, i) => (q, i)))
        {
            var interviewQuestionId = Guid.NewGuid();
            
            var interviewQuestion = new InterviewQuestion
            {
                Id = interviewQuestionId,
                InterviewSessionId = sessionId,
                Text = question.Text,
                Type = MapQuestionType(question.Type),
                ProgrammingLanguageCode = question.ProgrammingLanguageCode,
                OrderIndex = index,
                ReferenceSolution = question.ReferenceSolution,
                Status = QuestionStatus.NotStarted,
                OverallVerdict = Verdict.None,
                TimeLimitMs = question.TimeLimitMs,
                MemoryLimitMb = question.MemoryLimitMb,
                TestCases = MapTestCases(question.TestCases, interviewQuestionId)
            };

            interviewQuestions.Add(interviewQuestion);
        }

        session.Questions = interviewQuestions;
        
        await dbContext.InterviewSessions.AddAsync(session, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success(sessionId);
    }

    private static QuestionType MapQuestionType(QuestionBank.InternalApi.QuestionType type)
        => type switch
        {
            QuestionBank.InternalApi.QuestionType.Coding => QuestionType.Coding,
            QuestionBank.InternalApi.QuestionType.Theory => QuestionType.Theory,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    private static List<TestCase> MapTestCases(List<TestCaseApiDto> testCases, Guid interviewQuestionId)
    {
        return testCases
            .Select((tc, index) => new TestCase
            {
                Id = Guid.NewGuid(),
                InterviewQuestionId = interviewQuestionId,
                Input = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                IsHidden = tc.IsHidden,
                OrderIndex = index,
                ActualOutput = null,
                ExecutionTimeMs = null,
                MemoryUsedKb = null,
                Verdict = Verdict.None
            })
            .ToList();
    }
}