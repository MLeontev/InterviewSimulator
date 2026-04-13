using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess;

public class AppDbContext : DbContext, IDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<Question> Questions { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    public DbSet<CodingQuestionLanguageLimit> CodingQuestionLanguageLimits { get; set; }
    public DbSet<Competency> Competencies { get; set; }
    public DbSet<InterviewPresetCompetency> InterviewPresetCompetencies { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<Technology> Technologies { get; set; }
    public DbSet<InterviewPreset> InterviewPresets { get; set; }
    public DbSet<InterviewPresetTechnology> InterviewPresetTechnologies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.QuestionBank);
        
        modelBuilder.Entity<InterviewPreset>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<Grade>().HasIndex(g => g.Code).IsUnique();
        modelBuilder.Entity<Specialization>().HasIndex(s => s.Code).IsUnique();
        modelBuilder.Entity<Competency>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<Technology>().HasIndex(t => t.Code).IsUnique();

        modelBuilder.Entity<InterviewPresetCompetency>()
            .HasKey(pc => new { pc.InterviewPresetId, pc.CompetencyId });
        
        modelBuilder.Entity<CodingQuestionLanguageLimit>()
            .HasIndex(ll => new { ll.CodingQuestionId, ll.LanguageId })
            .IsUnique();
        
        modelBuilder.Entity<InterviewPresetTechnology>()
            .HasKey(pt => new { pt.InterviewPresetId, pt.TechnologyId });
        
        modelBuilder.Entity<Question>().Property(q => q.Type).HasConversion<string>();
    }
}