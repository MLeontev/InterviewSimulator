using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Interview.Infrastructure.Implementation.DataAccess;

public class AppDbContext : DbContext, IDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<InterviewSession> InterviewSessions { get; set; }
    public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Interview);
        
        modelBuilder.Entity<InterviewSession>()
            .Property(i => i.Status)
            .HasConversion<string>();
        
        modelBuilder.Entity<InterviewQuestion>()
            .Property(q => q.Status)
            .HasConversion<string>();
        
        modelBuilder.Entity<InterviewQuestion>()
            .Property(q => q.Type)
            .HasConversion<string>();
        
        modelBuilder.Entity<InterviewQuestion>()
            .Property(q => q.OverallVerdict)
            .HasConversion<string>();
        
        modelBuilder.Entity<TestCase>()
            .Property(q => q.Verdict)
            .HasConversion<string>();
    }
}