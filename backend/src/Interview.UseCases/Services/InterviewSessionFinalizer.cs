using Interview.Domain;
using Interview.Domain.Entities;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Services;

internal interface IInterviewSessionFinalizer
{
    Task TryFinishIfNoActiveQuestionsAsync(Guid sessionId, CancellationToken ct);
}

internal class InterviewSessionFinalizer(IDbContext dbContext) : IInterviewSessionFinalizer
{
    public async Task TryFinishIfNoActiveQuestionsAsync(Guid sessionId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        
        await dbContext.InterviewSessions
            .Where(x => x.Id == sessionId && x.Status == InterviewStatus.InProgress)
            .Where(x => !x.Questions
                .Any(q => InterviewQuestionStatusRules.Active.Contains(q.Status)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, InterviewStatus.Finished)
                .SetProperty(x => x.FinishedAt, now), ct);
    }
}
