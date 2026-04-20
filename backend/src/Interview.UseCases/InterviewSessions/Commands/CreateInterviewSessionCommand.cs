using Framework.Domain;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestionBank.ModuleContract;
using QuestionType = Interview.Domain.Enums.QuestionType;

namespace Interview.UseCases.InterviewSessions.Commands;

public record CreateInterviewSessionCommand(Guid CandidateId, Guid InterviewPresetId) : IRequest<Result<Guid>>;

internal class CreateInterviewSessionCommandHandler(
    IDbContext dbContext,
    IQuestionBankApi questionBankApi,
    IOptions<InterviewSessionQuestionSetOptions> questionSetOptions) : IRequestHandler<CreateInterviewSessionCommand, Result<Guid>>
{
    private readonly InterviewSessionQuestionSetOptions _questionSetOptions = questionSetOptions.Value;
    
    public async Task<Result<Guid>> Handle(CreateInterviewSessionCommand request, CancellationToken ct)
    {
        var theoryCount = _questionSetOptions.TheoryCount;
        var codingCount = _questionSetOptions.CodingCount;
        
        var hasActiveSession = await dbContext.InterviewSessions
            .AnyAsync(s => 
                s.CandidateId == request.CandidateId && 
                s.Status == InterviewStatus.InProgress, ct);

        if (hasActiveSession)
            return Result.Failure<Guid>(Error.Conflict("ACTIVE_SESSION_EXISTS", "У кандидата уже есть активная сессия интервью"));

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

        var interviewQuestions = questionSet.Questions
            .OrderBy(x => x.OrderIndex)
            .Select(q => InterviewQuestion.Create(
                sessionId: sessionId,
                title: q.Title,
                text: q.Text,
                type: MapQuestionType(q.Type),
                orderIndex: q.OrderIndex,
                referenceSolution: q.ReferenceSolution,
                competencyId: q.CompetencyId,
                competencyName: q.CompetencyName,
                programmingLanguageCode: q.ProgrammingLanguageCode,
                timeLimitMs: q.TimeLimitMs,
                memoryLimitMb: q.MemoryLimitMb,
                testCases: q.TestCases
                    .OrderBy(tc => tc.OrderIndex)
                    .Select(tc => TestCase.Create(
                        tc.Input,
                        tc.ExpectedOutput,
                        tc.IsHidden,
                        tc.OrderIndex))
                    .ToList()))
            .ToList();

        var session = InterviewSession.Create(
            sessionId,
            request.CandidateId,
            request.InterviewPresetId,
            presetInfo.Name,
            now,
            now.AddHours(1),
            interviewQuestions);
        
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
}
