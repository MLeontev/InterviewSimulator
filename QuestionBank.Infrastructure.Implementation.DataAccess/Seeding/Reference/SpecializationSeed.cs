using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

public static class SpecializationIds
{
    public static readonly Guid AlgorithmsAndDataStructures = Guid.Parse("00000002-0000-0000-0000-000000000001");
    public static readonly Guid Frontend = Guid.Parse("00000002-0000-0000-0000-000000000002");
    public static readonly Guid Backend = Guid.Parse("00000002-0000-0000-0000-000000000003");
}

public class SpecializationSeed : ISeed
{
    public int SeedOrder => SeedOrders.Specializations;
    
    public async Task SeedAsync(IDbContext dbContext, CancellationToken ct = default)
    {
        var specializations = new List<Specialization>
        {
            new()
            {
                Id = SpecializationIds.AlgorithmsAndDataStructures,
                Code = "algorithms-and-data-structures",
                Name = "Algorithms and Data Structures",
                Description = "Подготовка к техническим интервью с акцентом на алгоритмы, структуры данных и решение задач программирования"
            },
            new()
            {
                Id = SpecializationIds.Frontend,
                Code = "frontend",
                Name = "Фронтенд-разработка",
                Description = "Клиентская разработка пользовательских интерфейсов"
            },
            new()
            {
                Id = SpecializationIds.Backend,
                Code = "backend",
                Name = "Бэкенд-разработка",
                Description = "Серверная разработка бизнес-логики, API и интеграций"
            }
        };

        foreach (var specialization in specializations)
        {
            var existing = await dbContext.Specializations
                .FirstOrDefaultAsync(x => x.Id == specialization.Id || x.Code == specialization.Code, ct);

            if (existing is null)
            {
                dbContext.Specializations.Add(specialization);
                continue;
            }

            existing.Code = specialization.Code;
            existing.Name = specialization.Name;
            existing.Description = specialization.Description;
        }
    }
}
