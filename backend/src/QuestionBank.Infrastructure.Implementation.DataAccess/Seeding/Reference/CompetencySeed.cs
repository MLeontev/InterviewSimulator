using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

public static class CompetencyIds
{
    public static readonly Guid PythonCore = Guid.Parse("00000004-0000-0000-0000-000000000001");
    public static readonly Guid AlgorithmsBasic = Guid.Parse("00000004-0000-0000-0000-000000000002");
    public static readonly Guid DataStructures = Guid.Parse("00000004-0000-0000-0000-000000000003");
    public static readonly Guid CodingProblemSolving = Guid.Parse("00000004-0000-0000-0000-000000000004");
    public static readonly Guid TestingDebugging = Guid.Parse("00000004-0000-0000-0000-000000000005");
}

public class CompetencySeed : ISeed
{
    public int SeedOrder => SeedOrders.Competencies;

    public async Task SeedAsync(IDbContext dbContext, CancellationToken ct = default)
    {
        var competencies = new List<Competency>
        {
            new()
            {
                Id = CompetencyIds.PythonCore,
                Code = "python-core",
                Name = "Основы Python",
                Description = "Синтаксис Python, типы данных, коллекции, функции, comprehensions, исключения и базовые особенности языка"
            },
            new()
            {
                Id = CompetencyIds.AlgorithmsBasic,
                Code = "algorithms-basic",
                Name = "Базовые алгоритмы",
                Description = "Базовые алгоритмы, поиск, сортировка, оценка временной и пространственной сложности"
            },
            new()
            {
                Id = CompetencyIds.DataStructures,
                Code = "data-structures",
                Name = "Структуры данных",
                Description = "Списки, словари, множества, кортежи, стек, очередь и выбор подходящей структуры данных"
            },
            new()
            {
                Id = CompetencyIds.CodingProblemSolving,
                Code = "coding-problem-solving",
                Name = "Решение задач программированием",
                Description = "Практическое решение задач программированием, декомпозиция, реализация алгоритма и обработка граничных случаев"
            },
            new()
            {
                Id = CompetencyIds.TestingDebugging,
                Code = "testing-debugging",
                Name = "Тестирование и отладка",
                Description = "Проверка корректности решения, анализ ошибок, работа с тестовыми и граничными сценариями"
            }
        };

        foreach (var competency in competencies)
        {
            var existing = await dbContext.Competencies
                .FirstOrDefaultAsync(x => x.Id == competency.Id || x.Code == competency.Code, ct);

            if (existing is null)
            {
                dbContext.Competencies.Add(competency);
                continue;
            }

            existing.Code = competency.Code;
            existing.Name = competency.Name;
            existing.Description = competency.Description;
        }
    }
}
