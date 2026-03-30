using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;

namespace InterviewSimulator.API.SeedData;

public static class DbInitializer
{
    private static readonly Guid JuniorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MiddleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid SeniorId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    
    private static readonly Guid FrontendId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid BackendId = Guid.Parse("11111111-0000-0000-0000-000000000002");
    
    private static readonly Guid JavaScriptId = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly Guid CSharpId = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid ReactId = Guid.Parse("22222222-0000-0000-0000-000000000003");
    private static readonly Guid AspNetId = Guid.Parse("22222222-0000-0000-0000-000000000004");
    private static readonly Guid EfCoreId = Guid.Parse("22222222-0000-0000-0000-000000000005");
    private static readonly Guid PostgresId = Guid.Parse("22222222-0000-0000-0000-000000000006");
    
    private static readonly Guid PresetFrontendJuniorId = Guid.Parse("ffffffff-0000-0000-0000-000000000001");
    private static readonly Guid PresetBackendJuniorId = Guid.Parse("ffffffff-0000-0000-0000-000000000002");
    
    public static async Task SeedQuestionBankAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        await SeedGradesAsync(db);
        await SeedSpecializationsAsync(db);
        await SeedTechnologiesAsync(db);
        await SeedPresetsAsync(db);

        await SeedCompetenciesAsync(db);
    }

    private static async Task SeedGradesAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        var grades = new[]
        {
            new Grade { Id = JuniorId, Code = "junior", Name = "Junior" },
            new Grade { Id = MiddleId, Code = "middle", Name = "Middle" },
            new Grade { Id = SeniorId, Code = "senior", Name = "Senior" }
        };

        foreach (var g in grades)
            if (!await db.Grades.AnyAsync(x => x.Id == g.Id))
                db.Grades.Add(g);

        await db.SaveChangesAsync();
    }
    
    private static async Task SeedSpecializationsAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        var specs = new[]
        {
            new Specialization { Id = FrontendId, Code = "frontend", Name = "Frontend", Description = "Web UI" },
            new Specialization { Id = BackendId, Code = "backend", Name = "Backend", Description = "Server-side" }
        };

        foreach (var s in specs)
            if (!await db.Specializations.AnyAsync(x => x.Id == s.Id))
                db.Specializations.Add(s);
        
        await db.SaveChangesAsync();
    }
    
    private static async Task SeedTechnologiesAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        var techs = new[]
        {
            new Technology { Id = JavaScriptId, Name = "JavaScript", Code = "javascript", Category = TechnologyCategory.ProgrammingLanguage },
            new Technology { Id = CSharpId, Name = "C#", Code = "csharp", Category = TechnologyCategory.ProgrammingLanguage },
            new Technology { Id = ReactId, Name = "React", Code = "react", Category = TechnologyCategory.Framework },
            new Technology { Id = AspNetId, Name = "ASP.NET Core", Code = "aspnet-core", Category = TechnologyCategory.Framework },
            new Technology { Id = EfCoreId, Name = "Entity Framework Core", Code = "ef-core", Category = TechnologyCategory.ORM },
            new Technology { Id = PostgresId, Name = "PostgreSQL", Code = "postgresql", Category = TechnologyCategory.Database },
        };

        foreach (var t in techs)
        {
            if (!await db.Technologies.AnyAsync(x => x.Id == t.Id))
                db.Technologies.Add(t);
        }

        await db.SaveChangesAsync();
    }
    
    private static async Task SeedPresetsAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        if (!await db.InterviewPresets.AnyAsync(x => x.Id == PresetFrontendJuniorId))
        {
            var preset = new InterviewPreset
            {
                Id = PresetFrontendJuniorId,
                Code = "frontend-react-junior",
                Name = "Frontend React Junior",
                GradeId = JuniorId,
                SpecializationId = FrontendId
            };
            db.InterviewPresets.Add(preset);

            db.InterviewPresetTechnologies.AddRange(
                new() { InterviewPresetId = PresetFrontendJuniorId, TechnologyId = JavaScriptId },
                new() { InterviewPresetId = PresetFrontendJuniorId, TechnologyId = ReactId }
            );
        }

        if (!await db.InterviewPresets.AnyAsync(x => x.Id == PresetBackendJuniorId))
        {
            var preset = new InterviewPreset
            {
                Id = PresetBackendJuniorId,
                Code = "backend-dotnet-junior",
                Name = ".NET Backend Junior",
                GradeId = JuniorId,
                SpecializationId = BackendId
            };
            db.InterviewPresets.Add(preset);

            db.InterviewPresetTechnologies.AddRange(
                new() { InterviewPresetId = PresetBackendJuniorId, TechnologyId = CSharpId },
                new() { InterviewPresetId = PresetBackendJuniorId, TechnologyId = AspNetId },
                new() { InterviewPresetId = PresetBackendJuniorId, TechnologyId = EfCoreId },
                new() { InterviewPresetId = PresetBackendJuniorId, TechnologyId = PostgresId }
            );
        }

        await db.SaveChangesAsync();
    }
    
    private static async Task SeedCompetenciesAsync(QuestionBank.Infrastructure.Implementation.DataAccess.AppDbContext db)
    {
        if (db.Competencies.Any())
            return;
        
        var oop = new Competency
        {
            Id = Guid.NewGuid(),
            Code = "oop",
            Name = "OOP",
            Description = "Объектно-ориентированное программирование: классы, наследование, интерфейсы"
        };

        var collections = new Competency
        {
            Id = Guid.NewGuid(),
            Code = "collections",
            Name = "Collections",
            Description = "Списки, словари, массивы, IEnumerable/IEnumerator"
        };

        var dbKnowledge = new Competency
        {
            Id = Guid.NewGuid(),
            Code = "databases",
            Name = "Databases",
            Description = "Основы работы с реляционными БД, SQL, Entity Framework"
        };

        var algorithms = new Competency
        {
            Id = Guid.NewGuid(),
            Code = "algorithms",
            Name = "Algorithms",
            Description = "Алгоритмы, структуры данных, базовые задачи на массивы, строки, списки"
        };

        await db.AddRangeAsync(oop, collections, dbKnowledge, algorithms);

        var gradeId = JuniorId;
        var csharpTech = await db.Technologies.FirstAsync(t => t.Code == "csharp");

        var presetCompetencies = new[]
        {
            new InterviewPresetCompetency
            {
                Id = Guid.NewGuid(),
                InterviewPresetId = PresetBackendJuniorId,
                CompetencyId = oop.Id,
                Weight = 0.3
            },
            new InterviewPresetCompetency
            {
                Id = Guid.NewGuid(),
                InterviewPresetId = PresetBackendJuniorId,
                CompetencyId = collections.Id,
                Weight = 0.2
            },
            new InterviewPresetCompetency
            {
                Id = Guid.NewGuid(),
                InterviewPresetId = PresetBackendJuniorId,
                CompetencyId = dbKnowledge.Id,
                Weight = 0.2
            },
            new InterviewPresetCompetency
            {
                Id = Guid.NewGuid(),
                InterviewPresetId = PresetBackendJuniorId,
                CompetencyId = algorithms.Id,
                Weight = 0.3
            }
        };

        foreach (var pc in presetCompetencies)
        {
            if (!await db.InterviewPresetCompetencies.AnyAsync(x =>
                    x.InterviewPresetId == pc.InterviewPresetId && x.CompetencyId == pc.CompetencyId))
            {
                db.InterviewPresetCompetencies.Add(pc);
            }
        }

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
