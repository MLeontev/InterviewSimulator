using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

public static class TechnologyIds
{
    public static readonly Guid Python = Guid.Parse("00000003-0000-0000-0000-000000000001");
}

public class TechnologySeed : ISeed
{
    public int SeedOrder => SeedOrders.Technologies;

    public async Task SeedAsync(IDbContext dbContext, CancellationToken ct = default)
    {
        var technologies = new List<Technology>
        {
            new()
            {
                Id = TechnologyIds.Python,
                Code = "python",
                Name = "Python",
                Category = TechnologyCategory.ProgrammingLanguage,
                Description = "Интерпретируемый язык программирования общего назначения"
            }
        };

        foreach (var technology in technologies)
        {
            var existing = await dbContext.Technologies
                .FirstOrDefaultAsync(x => x.Id == technology.Id || x.Code == technology.Code, ct);

            if (existing is null)
            {
                dbContext.Technologies.Add(technology);
                continue;
            }

            existing.Code = technology.Code;
            existing.Name = technology.Name;
            existing.Category = technology.Category;
            existing.Description = technology.Description;
        }
    }
}
