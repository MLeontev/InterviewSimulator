using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

public static class TechnologyIds
{
    public static readonly Guid Python = Guid.Parse("00000003-0000-0000-0000-000000000001");
    public static readonly Guid CSharp = Guid.Parse("00000003-0000-0000-0000-000000000002");
    public static readonly Guid AspNetCore = Guid.Parse("00000003-0000-0000-0000-000000000003");
    public static readonly Guid EfCore = Guid.Parse("00000003-0000-0000-0000-000000000004");
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
            },
            new()
            {
                Id = TechnologyIds.CSharp,
                Code = "csharp",
                Name = "C#",
                Category = TechnologyCategory.ProgrammingLanguage,
                Description = "Объектно-ориентированный язык программирования платформы .NET"
            },
            new()
            {
                Id = TechnologyIds.AspNetCore,
                Code = "aspnet-core",
                Name = "ASP.NET Core",
                Category = TechnologyCategory.Framework,
                Description = "Фреймворк для разработки веб-приложений и HTTP API на платформе .NET"
            },
            new()
            {
                Id = TechnologyIds.EfCore,
                Code = "ef-core",
                Name = "Entity Framework Core",
                Category = TechnologyCategory.ORM,
                Description = "ORM для работы с реляционными базами данных в приложениях .NET"
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
