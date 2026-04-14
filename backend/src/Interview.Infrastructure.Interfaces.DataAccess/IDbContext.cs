using Interview.Domain;
using Interview.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Interview.Infrastructure.Interfaces.DataAccess;

public interface IDbContext
{
    public DbSet<InterviewSession> InterviewSessions { get; set; }
    public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    
    void AddOutboxMessage(object @event);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}