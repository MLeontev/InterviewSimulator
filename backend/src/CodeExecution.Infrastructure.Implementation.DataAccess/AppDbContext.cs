using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using Framework.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CodeExecution.Infrastructure.Implementation.DataAccess;

public class AppDbContext : ModuleDbContext, IDbContext
{
    protected override string Schema => Schemas.CodeExecution;
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<CodeSubmission> CodeSubmissions { get; set; }
    public DbSet<CodeSubmissionTestCase> CodeSubmissionTestCases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema(Schemas.CodeExecution);

        modelBuilder.Entity<CodeSubmission>()
            .Property(cs => cs.Status)
            .HasConversion<string>();
        
        modelBuilder.Entity<CodeSubmission>()
            .Property(cs => cs.OverallVerdict)
            .HasConversion<string>();
        
        modelBuilder.Entity<CodeSubmissionTestCase>()
            .HasIndex(x => new { x.SubmissionId, x.OrderIndex })
            .IsUnique();
        
        modelBuilder.Entity<CodeSubmissionTestCase>()
            .Property(tc => tc.Verdict)
            .HasConversion<string>();
    }
}