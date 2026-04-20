using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using InterviewQuestionEntity = Interview.Domain.Entities.InterviewQuestion;

namespace Interview.UseCases.Services;

internal interface ICurrentQuestionResolver
{
    Task<InterviewQuestionEntity?> GetCurrentQuestionAsync(Guid candidateId, CancellationToken ct);
}

internal class CurrentQuestionResolver(
    IDbContext dbContext, 
    ICurrentSessionResolver currentSessionResolver) : ICurrentQuestionResolver
{
    public async Task<InterviewQuestionEntity?> GetCurrentQuestionAsync(Guid candidateId, CancellationToken ct)
    {
        var sessionId = await currentSessionResolver.GetCurrentSessionIdAsync(candidateId, ct);
        if (sessionId is null)
            return  null;
        
        return await dbContext.InterviewQuestions
            .Include(q => q.InterviewSession)
            .Include(q => q.TestCases)
            .Where(q => q.InterviewSessionId == sessionId.Value)
            .Where(q => !InterviewQuestionStatusRules.Terminal.Contains(q.Status))
            .OrderBy(q => q.OrderIndex)
            .FirstOrDefaultAsync(ct);
    }
}
