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
    public DbSet<CompetencyMatrix> CompetencyMatrices { get; set; }
    public DbSet<CompetencyMatrixItem> CompetencyMatrixItems { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<Technology> Technologies { get; set; }
    public DbSet<InterviewPreset> InterviewPresets { get; set; }
    public DbSet<InterviewPresetTechnology> InterviewPresetTechnologies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.QuestionBank);
        
        modelBuilder.Entity<Question>().Property(q => q.Type).HasConversion<string>();

        modelBuilder.Entity<CompetencyMatrix>()
            .HasIndex(c => new { c.GradeId, c.SpecializationId })
            .IsUnique();
        
        modelBuilder.Entity<InterviewPresetTechnology>()
            .HasKey(pt => new { pt.InterviewPresetId, pt.TechnologyId });
    }
}