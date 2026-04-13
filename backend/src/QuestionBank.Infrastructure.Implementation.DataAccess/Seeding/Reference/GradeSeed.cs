using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

public static class GradeIds
{
    public static readonly Guid Junior = Guid.Parse("00000001-0000-0000-0000-000000000001");
    public static readonly Guid Middle = Guid.Parse("00000001-0000-0000-0000-000000000002");
    public static readonly Guid Senior = Guid.Parse("00000001-0000-0000-0000-000000000003");
}

public class GradeSeed : ISeed
{
    public int SeedOrder => SeedOrders.Grades;
    
    public async Task SeedAsync(IDbContext dbContext, CancellationToken ct = default)
    {
        var grades = new List<Grade>
        {
            new() { Id = GradeIds.Junior, Code = "junior", Name = "Junior", Description = "0–1 год опыта" },
            new() { Id = GradeIds.Middle, Code = "middle", Name = "Middle", Description = "1–3 года опыта" },
            new() { Id = GradeIds.Senior, Code = "senior", Name = "Senior", Description = "3+ лет опыта" },
        };

        foreach (var grade in grades)
        {
            var existing = await dbContext.Grades
                .FirstOrDefaultAsync(x => x.Id == grade.Id || x.Code == grade.Code, ct);

            if (existing is null)
            {
                dbContext.Grades.Add(grade);
                continue;
            }

            existing.Code = grade.Code;
            existing.Name = grade.Name;
            existing.Description = grade.Description;
        }
    }
}
