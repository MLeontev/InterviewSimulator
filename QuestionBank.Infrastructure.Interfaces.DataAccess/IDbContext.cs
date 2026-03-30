using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;

namespace QuestionBank.Infrastructure.Interfaces.DataAccess;

public interface IDbContext
{
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
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}