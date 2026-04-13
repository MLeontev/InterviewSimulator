using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Services;

internal interface IInterviewSessionFinalizer
{
    Task TryFinishIfNoActiveQuestionsAsync(Guid sessionId, CancellationToken ct);
}

internal class InterviewSessionFinalizer(IDbContext dbContext) : IInterviewSessionFinalizer
{
    private static readonly HashSet<QuestionStatus> ActiveStatuses =
    [
        QuestionStatus.NotStarted,
        QuestionStatus.InProgress,
        QuestionStatus.EvaluatingCode,
        QuestionStatus.EvaluatedCode
    ];
    
    public async Task TryFinishIfNoActiveQuestionsAsync(Guid sessionId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        
        await dbContext.InterviewSessions
            .Where(x => x.Id == sessionId && x.Status == InterviewStatus.InProgress)
            .Where(x => !x.Questions
                .Any(q => ActiveStatuses.Contains(q.Status)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, InterviewStatus.Finished)
                .SetProperty(x => x.FinishedAt, now), ct);
    }
}