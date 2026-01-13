using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodeExecution.Infrastructure.Implementation.DataAccess;

internal class AppDbContext : DbContext, IDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<CodeSubmission> CodeSubmissions { get; set; }
    public DbSet<CodeSubmissionTestCase> CodeSubmissionTestCases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.CodeExecution);

        modelBuilder.Entity<CodeSubmission>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
        
        modelBuilder.Entity<CodeSubmissionTestCase>(entity =>
        {
            entity.Property(e => e.Verdict).HasConversion<string>();
        });
    }
}