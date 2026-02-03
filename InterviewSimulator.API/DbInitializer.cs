using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;

public static class DbInitializer
{
    public static async Task SeedPresetsAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        if (db.InterviewPresets.Any())
            return;

        // Grades
        var junior = new Grade
        {
            Id = Guid.NewGuid(),
            Name = "Junior",
            Description = ""
        };
        
        var middle = new Grade
        {
            Id = Guid.NewGuid(),
            Name = "Middle",
            Description = ""
        };
        
        var senior = new Grade
        {
            Id = Guid.NewGuid(),
            Name = "Senior",
            Description = ""
        };

        // Specializations
        var frontend = new Specialization
        {
            Id = Guid.NewGuid(),
            Name = "Frontend",
            Description = "Web UI development"
        };

        var backend = new Specialization
        {
            Id = Guid.NewGuid(),
            Name = "Backend",
            Description = "Server-side development"
        };

        // Technologies
        var javascript = new Technology
        {
            Id = Guid.NewGuid(),
            Name = "JavaScript",
            Category = TechnologyCategory.ProgrammingLanguage,
            Code = "javascript"
        };

        var csharp = new Technology
        {
            Id = Guid.NewGuid(),
            Name = "C#",
            Category = TechnologyCategory.ProgrammingLanguage,
            Code = "csharp"
        };

        var react = new Technology
        {
            Id = Guid.NewGuid(),
            Name = "React",
            Category = TechnologyCategory.Framework
        };

        var aspNet = new Technology
        {
            Id = Guid.NewGuid(),
            Name = "ASP.NET Core",
            Category = TechnologyCategory.Framework
        };

        var efCore = new Technology
        {
            Id = Guid.NewGuid(),
            Name = "Entity Framework Core",
            Category = TechnologyCategory.ORM
        };

        var postgres = new Technology
        {
            Id = Guid.NewGuid(),
            Name = "PostgreSQL",
            Category = TechnologyCategory.Database
        };

        await db.AddRangeAsync(
            junior,
            middle,
            senior,
            frontend,
            backend,
            javascript,
            csharp,
            react,
            aspNet,
            efCore,
            postgres
        );

        // InterviewPreset — Frontend
        var frontendPreset = new InterviewPreset
        {
            Id = Guid.NewGuid(),
            Name = "Frontend React Junior",
            Grade = junior,
            Specialization = frontend,
            Technologies =
            {
                new InterviewPresetTechnology { Technology = javascript },
                new InterviewPresetTechnology { Technology = react }
            }
        };

        // InterviewPreset — Backend
        var backendPreset = new InterviewPreset
        {
            Id = Guid.NewGuid(),
            Name = ".NET Backend Junior",
            Grade = junior,
            Specialization = backend,
            Technologies =
            {
                new InterviewPresetTechnology { Technology = csharp },
                new InterviewPresetTechnology { Technology = aspNet },
                new InterviewPresetTechnology { Technology = efCore },
                new InterviewPresetTechnology { Technology = postgres }
            }
        };

        await db.AddRangeAsync(frontendPreset, backendPreset);

        await db.SaveChangesAsync();
    }
    
    public static async Task SeedCompetenciesAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        if (db.Competencies.Any())
            return;
        
        var oop = new Competency
        {
            Id = Guid.NewGuid(),
            Name = "OOP",
            Description = "Объектно-ориентированное программирование: классы, наследование, интерфейсы"
        };

        var collections = new Competency
        {
            Id = Guid.NewGuid(),
            Name = "Collections",
            Description = "Списки, словари, массивы, IEnumerable/IEnumerator"
        };

        var dbKnowledge = new Competency
        {
            Id = Guid.NewGuid(),
            Name = "Databases",
            Description = "Основы работы с реляционными БД, SQL, Entity Framework"
        };

        var algorithms = new Competency
        {
            Id = Guid.NewGuid(),
            Name = "Algorithms",
            Description = "Алгоритмы, структуры данных, базовые задачи на массивы, строки, списки"
        };

        await db.AddRangeAsync(oop, collections, dbKnowledge, algorithms);
        
        var juniorBackendGrade = await db.Grades.FirstAsync(g => g.Name == "Junior");
        var backendSpec = await db.Specializations.FirstAsync(s => s.Name == "Backend");

        var matrix = new CompetencyMatrix
        {
            Id = Guid.NewGuid(),
            GradeId = juniorBackendGrade.Id,
            SpecializationId = backendSpec.Id,
            Competencies =
            {
                new CompetencyMatrixItem { Id = Guid.NewGuid(), Competency = oop, Weight = 0.3 },
                new CompetencyMatrixItem { Id = Guid.NewGuid(), Competency = collections, Weight = 0.2 },
                new CompetencyMatrixItem { Id = Guid.NewGuid(), Competency = dbKnowledge, Weight = 0.2 },
                new CompetencyMatrixItem { Id = Guid.NewGuid(), Competency = algorithms, Weight = 0.3 }
            }
        };

        await db.AddAsync(matrix);
        
        var gradeId = juniorBackendGrade.Id;
        var csharpTech = await db.Technologies.FirstAsync(t => t.Code == "csharp");

        var q1 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Что такое наследование в C#?",
            Type = QuestionType.Theory,
            CompetencyId = oop.Id,
            GradeId = gradeId,
            TechnologyId = csharpTech.Id
        };

        var q2 = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Объясните разницу между List<T> и IEnumerable<T>.",
            Type = QuestionType.Theory,
            CompetencyId = collections.Id,
            GradeId = gradeId,
            TechnologyId = csharpTech.Id
        };

        var codingQuestion = new Question
        {
            Id = Guid.NewGuid(),
            Text = "Напишите метод, который суммирует все элементы int[] и возвращает результат.",
            Type = QuestionType.Coding,
            CompetencyId = algorithms.Id,
            GradeId = gradeId,
            TechnologyId = csharpTech.Id,
            TestCases =
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    Input = "[1,2,3]",
                    ExpectedOutput = "6",
                    IsHidden = false,
                    OrderIndex = 1
                },
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    Input = "[]",
                    ExpectedOutput = "0",
                    IsHidden = false,
                    OrderIndex = 2
                }
            },
            LanguageLimits =
            {
                new CodingQuestionLanguageLimit
                {
                    Id = Guid.NewGuid(),
                    LanguageId = csharpTech.Id,
                    TimeLimitMs = 1000,
                    MemoryLimitMb = 64
                }
            }
        };

        await db.AddRangeAsync(q1, q2, codingQuestion);

        await db.SaveChangesAsync();
    }
}
