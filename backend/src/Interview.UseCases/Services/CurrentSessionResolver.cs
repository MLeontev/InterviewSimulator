using Interview.Domain;
using Interview.Domain.Entities;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Services;

internal interface ICurrentSessionResolver
{
    Task<InterviewSession?> GetCurrentSessionAsync(Guid candidateId, CancellationToken ct);
    Task<Guid?> GetCurrentSessionIdAsync(Guid candidateId, CancellationToken ct);
}

internal class CurrentSessionResolver(IDbContext dbContext) : ICurrentSessionResolver
{
    public async Task<InterviewSession?> GetCurrentSessionAsync(Guid candidateId, CancellationToken ct) => 
        await dbContext.InterviewSessions
            .Where(s => s.CandidateId == candidateId && s.Status == InterviewStatus.InProgress)
            .Include(x => x.Questions)
            .SingleOrDefaultAsync(ct);
    
    public Task<Guid?> GetCurrentSessionIdAsync(Guid candidateId, CancellationToken ct) => 
        dbContext.InterviewSessions
            .Where(s => s.CandidateId == candidateId && s.Status == InterviewStatus.InProgress)
            .Select(s => (Guid?)s.Id)
            .SingleOrDefaultAsync(ct);
}