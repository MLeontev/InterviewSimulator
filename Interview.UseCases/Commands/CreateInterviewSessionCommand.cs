using Framework.Domain;
using MediatR;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using QuestionBank.ModuleContract;
using QuestionType = Interview.Domain.QuestionType;

namespace Interview.UseCases.Commands;

public record CreateInterviewSessionCommand(Guid CandidateId, Guid InterviewPresetId) : IRequest<Result<Guid>>;

internal class CreateInterviewSessionCommandHandler(
    IDbContext dbContext,
    IQuestionBankApi questionBankApi) : IRequestHandler<CreateInterviewSessionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInterviewSessionCommand request, CancellationToken ct)
    {
        const int theoryCount = 4;
        const int codingCount = 2;
        
        var hasActiveSession = await dbContext.InterviewSessions
            .AnyAsync(s => 
                s.CandidateId == request.CandidateId && 
                s.Status == InterviewStatus.InProgress, ct);

        if (hasActiveSession)
        {
            return Result.Failure<Guid>(Error.Business("ACTIVE_SESSION_EXISTS", "У кандидата уже есть активная сессия интервью"));
        }

        InterviewPresetApiDto? presetInfo;
        GeneratedQuestionSet questionSet;

        try
        {
            presetInfo = await questionBankApi.GetPresetAsync(request.InterviewPresetId);
            questionSet = await questionBankApi.GenerateInterviewQuestionsAsync(
                request.InterviewPresetId,
                theoryCount,
                codingCount,
                ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<Guid>(Error.External("QUESTION_BANK_ERROR", ex.Message));
        }

        if (presetInfo is null)
            return Result.Failure<Guid>(Error.NotFound("PRESET_NOT_FOUND", "Пресет интервью не найден"));

        if (questionSet.Questions.Count == 0)
            return Result.Failure<Guid>(Error.Business("NO_QUESTIONS", "Не удалось получить вопросы для сессии интервью"));

        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var session = new InterviewSession
        {
            Id = sessionId,
            CandidateId = request.CandidateId,
            InterviewPresetId = request.InterviewPresetId,
            InterviewPresetName = presetInfo.Name,
            StartedAt = now,
            PlannedEndAt = now.AddHours(1),
            Status = InterviewStatus.InProgress,
            Questions = []
        };

        var interviewQuestions = questionSet.Questions
            .OrderBy(x => x.OrderIndex)
            .Select(question =>
            {
                var interviewQuestionId = Guid.NewGuid();

                return new InterviewQuestion
                {
                    Id = interviewQuestionId,
                    InterviewSessionId = sessionId,
                    Text = question.Text,
                    Type = MapQuestionType(question.Type),
                    ProgrammingLanguageCode = question.ProgrammingLanguageCode,
                    OrderIndex = question.OrderIndex,
                    ReferenceSolution = question.ReferenceSolution,
                    Status = QuestionStatus.NotStarted,
                    OverallVerdict = Verdict.None,
                    TimeLimitMs = question.TimeLimitMs,
                    MemoryLimitMb = question.MemoryLimitMb,
                    TestCases = MapTestCases(question.TestCases, interviewQuestionId)
                };
            })
            .ToList();

        session.Questions = interviewQuestions;
        
        await dbContext.InterviewSessions.AddAsync(session, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success(sessionId);
    }

    private static QuestionType MapQuestionType(QuestionBank.ModuleContract.QuestionType type)
        => type switch
        {
            QuestionBank.ModuleContract.QuestionType.Coding => QuestionType.Coding,
            QuestionBank.ModuleContract.QuestionType.Theory => QuestionType.Theory,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    private static List<TestCase> MapTestCases(IReadOnlyCollection<GeneratedTestCase> testCases, Guid interviewQuestionId)
    {
        return testCases
            .OrderBy(tc => tc.OrderIndex)
            .Select(tc => new TestCase
            {
                Id = Guid.NewGuid(),
                InterviewQuestionId = interviewQuestionId,
                Input = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                IsHidden = tc.IsHidden,
                OrderIndex = tc.OrderIndex,
                ActualOutput = null,
                ExecutionTimeMs = null,
                MemoryUsedMb = null,
                Verdict = Verdict.None
            })
            .ToList();
    }
}
