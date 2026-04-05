using Interview.Domain;
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
    private static readonly HashSet<QuestionStatus> TerminalStatuses =
    [
        QuestionStatus.Skipped,
        QuestionStatus.Submitted,
        QuestionStatus.EvaluatedAi
    ];
    
    public async Task<InterviewQuestion?> GetCurrentQuestionAsync(Guid candidateId, CancellationToken ct)
    {
        var sessionId = await currentSessionResolver.GetCurrentSessionIdAsync(candidateId, ct);
        if (sessionId is null)
            return  null;
        
        return await dbContext.InterviewQuestions
            .Include(q => q.InterviewSession)
            .Include(q => q.TestCases)
            .Where(q => q.InterviewSessionId == sessionId.Value)
            .Where(q => !TerminalStatuses.Contains(q.Status))
            .OrderBy(q => q.OrderIndex)
            .FirstOrDefaultAsync(ct);
    }
}
