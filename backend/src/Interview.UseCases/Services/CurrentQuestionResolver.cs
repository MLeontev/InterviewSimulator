using Interview.Domain;
using Interview.Domain.Entities;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Services;

internal interface ICurrentQuestionResolver
{
    Task<InterviewQuestion?> GetCurrentQuestionAsync(Guid candidateId, CancellationToken ct);
}

internal class CurrentQuestionResolver(
    IDbContext dbContext, 
    ICurrentSessionResolver currentSessionResolver) : ICurrentQuestionResolver
{
    public async Task<InterviewQuestion?> GetCurrentQuestionAsync(Guid candidateId, CancellationToken ct)
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
