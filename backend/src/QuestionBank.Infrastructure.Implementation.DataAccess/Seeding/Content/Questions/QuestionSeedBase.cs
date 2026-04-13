using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions;

public abstract class QuestionSeedBase : ISeed
{
    public int SeedOrder => SeedOrders.Questions;
    
    protected abstract IReadOnlyCollection<QuestionDefinition> Questions { get; }
    
    public async Task SeedAsync(IDbContext dbContext, CancellationToken ct = default)
    {
        foreach (var definition in Questions)
        {
            var question = await dbContext.Questions
                .Include(x => x.LanguageLimits)
                .Include(x => x.TestCases)
                .FirstOrDefaultAsync(x => x.Id == definition.Id , ct);

            if (question is null)
            {
                question = new Question
                {
                    Id = definition.Id,
                    Title = definition.Title,
                    Text = definition.Text,
                    Type = definition.Type,
                    ReferenceSolution = definition.ReferenceSolution,
                    CompetencyId = definition.CompetencyId,
                    GradeId = definition.GradeId,
                    TechnologyId = definition.TechnologyId
                };

                dbContext.Questions.Add(question);
            }
            else
            {
                question.Title = definition.Title;
                question.Text = definition.Text;
                question.Type = definition.Type;
                question.ReferenceSolution = definition.ReferenceSolution;
                question.CompetencyId = definition.CompetencyId;
                question.GradeId = definition.GradeId;
                question.TechnologyId = definition.TechnologyId;
            }
            
            if (definition.Type == QuestionType.Coding)
            {
                SyncLanguageLimits(dbContext, question, definition.LanguageLimits ?? []);
                SyncTestCases(dbContext, question, definition.TestCases ?? []);
            }
            else
            {
                ClearCodingData(dbContext, question);
            }
        }
    }

    private static void SyncLanguageLimits(IDbContext dbContext, Question question, IReadOnlyCollection<LanguageLimitDefinition> requiredLimits)
    {
        var existingLimits = question.LanguageLimits.ToList();
        var requiredByLanguageId = requiredLimits.ToDictionary(x => x.LanguageId);

        foreach (var existingLimit in existingLimits)
        {
            if (!requiredByLanguageId.ContainsKey(existingLimit.LanguageId))
                dbContext.CodingQuestionLanguageLimits.Remove(existingLimit);
        }

        foreach (var required in requiredLimits)
        {
            var existing = existingLimits.FirstOrDefault(x => x.LanguageId == required.LanguageId);

            if (existing is null)
            {
                dbContext.CodingQuestionLanguageLimits.Add(new CodingQuestionLanguageLimit
                {
                    Id = Guid.NewGuid(),
                    CodingQuestionId = question.Id,
                    LanguageId = required.LanguageId,
                    TimeLimitMs = required.TimeLimitMs,
                    MemoryLimitMb = required.MemoryLimitMb
                });

                continue;
            }

            existing.TimeLimitMs = required.TimeLimitMs;
            existing.MemoryLimitMb = required.MemoryLimitMb;
        }
    }

    private static void SyncTestCases(IDbContext dbContext, Question question, IReadOnlyCollection<TestCaseDefinition> requiredTestCases)
    {
        var existingCases = question.TestCases.ToList();
        var requiredByOrder = requiredTestCases.ToDictionary(x => x.OrderIndex);

        foreach (var existingCase in existingCases)
        {
            if (!requiredByOrder.ContainsKey(existingCase.OrderIndex))
                dbContext.TestCases.Remove(existingCase);
        }

        foreach (var required in requiredTestCases)
        {
            var existing = existingCases.FirstOrDefault(x => x.OrderIndex == required.OrderIndex);

            if (existing is null)
            {
                dbContext.TestCases.Add(new TestCase
                {
                    Id = Guid.NewGuid(),
                    CodingQuestionId = question.Id,
                    Input = required.Input,
                    ExpectedOutput = required.ExpectedOutput,
                    IsHidden = required.IsHidden,
                    OrderIndex = required.OrderIndex
                });

                continue;
            }

            existing.Input = required.Input;
            existing.ExpectedOutput = required.ExpectedOutput;
            existing.IsHidden = required.IsHidden;
            existing.OrderIndex = required.OrderIndex;
        }
    }

    private void ClearCodingData(IDbContext dbContext, Question question)
    {
        foreach (var languageLimit in question.LanguageLimits)
            dbContext.CodingQuestionLanguageLimits.Remove(languageLimit);
        
        foreach (var testCase in question.TestCases)
            dbContext.TestCases.Remove(testCase);
    }


    protected record QuestionDefinition(
        Guid Id,
        string Title,
        string Text,
        QuestionType Type,
        string ReferenceSolution,
        Guid CompetencyId,
        Guid GradeId,
        Guid? TechnologyId,
        IReadOnlyCollection<LanguageLimitDefinition>? LanguageLimits = null,
        IReadOnlyCollection<TestCaseDefinition>? TestCases = null);

    protected record LanguageLimitDefinition(
        Guid LanguageId,
        int TimeLimitMs,
        int MemoryLimitMb);

    protected record TestCaseDefinition(
        string Input,
        string ExpectedOutput,
        bool IsHidden,
        int OrderIndex);
}