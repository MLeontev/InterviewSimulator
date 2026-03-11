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

        var prototypes = questions
            .Select(q => new InterviewQuestionPrototype(q))
            .ToList();

        var interviewQuestions = prototypes
            .Select((prototype, index) => prototype.CloneForSession(sessionId, index))
            .ToList();

        var session = new InterviewSessionBuilder()
            .WithSessionId(sessionId)
            .ForCandidate(request.CandidateId)
            .WithPresetName(presetInfo.Name)
            .StartsAt(now)
            .WithDuration(TimeSpan.FromHours(1))
            .WithQuestions(interviewQuestions)
            .InProgress()
            .Build();
        
        await dbContext.InterviewSessions.AddAsync(session, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success(sessionId);
    }
}