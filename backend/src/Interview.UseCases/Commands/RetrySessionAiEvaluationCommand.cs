using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record RetrySessionAiEvaluationCommand(Guid CandidateId, Guid SessionId) : IRequest<Result>;

internal class RetrySessionAiEvaluationCommandHandler(
    IDbContext dbContext) : IRequestHandler<RetrySessionAiEvaluationCommand, Result>
{
    public async Task<Result> Handle(RetrySessionAiEvaluationCommand request, CancellationToken cancellationToken)
    {
        var session = await dbContext.InterviewSessions
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x =>
                x.Id == request.SessionId &&
                x.CandidateId == request.CandidateId, cancellationToken);
        
        if (session is null)
            return Result.Failure(Error.NotFound("SESSION_NOT_FOUND", "Сессия не найдена"));

        if (session.Status != InterviewStatus.AiEvaluationFailed)
            return Result.Failure(Error.Business("SESSION_NOT_FAILED", "ИИ-оценка сессии не была завершена с ошибкой"));

        var now = DateTime.UtcNow;
        var retriedQuestions = 0;

        foreach (var q in session.Questions
                     .Where(x => x.Status == QuestionStatus.AiEvaluationFailed))
        {
            if (string.IsNullOrWhiteSpace(q.Answer))
                continue;
            
            q.Status = QuestionStatus.Submitted;
            q.SubmittedAt = now;
            q.EvaluatedAt = null;
            q.AiRetryCount = 0;
            q.AiNextRetryAt = null;
            q.AiFeedbackJson = null;
            q.ErrorMessage = null;
            q.QuestionVerdict = QuestionVerdict.None;
            
            retriedQuestions++;
        }
        
        if (retriedQuestions == 0)
            return Result.Failure(Error.Business("NO_FAILED_AI_QUESTIONS", "Нет заданий с ошибкой ИИ-оценки"));
        
        session.Status = InterviewStatus.Finished;
        session.AiRetryCount = 0;
        session.AiNextRetryAt = null;
        session.AiFeedbackJson = null;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}