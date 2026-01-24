using Interview.Domain;
using Microsoft.EntityFrameworkCore;

namespace Interview.Infrastructure.Interfaces.DataAccess;

public interface IDbContext
{
    public DbSet<InterviewSession> InterviewSessions { get; set; }
    public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}