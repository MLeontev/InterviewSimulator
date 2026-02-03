using Framework.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.UseCases.Queries;

public record GenerateInterviewQuestionsQuery(Guid InterviewPresetId, int TotalQuestions) : IRequest<Result<IReadOnlyList<InterviewQuestionDto>>>;

internal class GenerateInterviewQuestionsHandler(IDbContext dbContext) : IRequestHandler<GenerateInterviewQuestionsQuery, Result<IReadOnlyList<InterviewQuestionDto>>>
{
    public async Task<Result<IReadOnlyList<InterviewQuestionDto>>> Handle(GenerateInterviewQuestionsQuery request, CancellationToken ct)
    {
        var preset = await dbContext.InterviewPresets
            .Include(p => p.Technologies)
            .ThenInclude(pt => pt.Technology)
            .FirstOrDefaultAsync(p => p.Id == request.InterviewPresetId, ct);

        if (preset == null)
            return Result.Failure<IReadOnlyList<InterviewQuestionDto>>(
                Error.NotFound("PRESET_NOT_FOUND", "Пресет интервью не найден"));

        var programmingLanguages = preset.Technologies
            .Where(t => t.Technology.Category == TechnologyCategory.ProgrammingLanguage)
            .Select(t => t.Technology)
            .ToList();

        var language = programmingLanguages.FirstOrDefault();

        var matrix = await dbContext.CompetencyMatrices
            .Include(m => m.Competencies)
            .ThenInclude(c => c.Competency)
            .Where(m => m.GradeId == preset.GradeId && m.SpecializationId == preset.SpecializationId)
            .FirstOrDefaultAsync(ct);

        if (matrix == null)
            return Result.Failure<IReadOnlyList<InterviewQuestionDto>>(
                Error.NotFound("COMPETENCY_MATRIX_NOT_FOUND", "Матрица компетенций не найдена"));

        var result = new List<InterviewQuestionDto>();
        var takenQuestions = new HashSet<Guid>();
        var presetTechIds = preset.Technologies.Select(t => t.TechnologyId).ToList();

        foreach (var competency in matrix.Competencies)
        {
            var takeCount = (int)Math.Floor(competency.Weight * request.TotalQuestions);
            if (takeCount <= 0) continue;

            var questionsQuery = dbContext.Questions
                .Include(q => q.TestCases)
                .Include(q => q.LanguageLimits)
                .Where(q => q.CompetencyId == competency.CompetencyId
                            && q.GradeId == preset.GradeId
                            && presetTechIds.Contains(q.TechnologyId)
                            && !takenQuestions.Contains(q.Id));

            var questions = await questionsQuery
                .OrderBy(_ => Guid.NewGuid())
                .Take(takeCount)
                .ToListAsync(ct);

            foreach (var q in questions)
            {
                takenQuestions.Add(q.Id);
                result.Add(MapToDto(q, language));
            }
        }

        var missing = request.TotalQuestions - result.Count;
        if (missing > 0)
        {
            var extra = await dbContext.Questions
                .Include(q => q.TestCases)
                .Include(q => q.LanguageLimits)
                .Where(q => q.GradeId == preset.GradeId
                            && presetTechIds.Contains(q.TechnologyId)
                            && !takenQuestions.Contains(q.Id))
                .OrderBy(_ => Guid.NewGuid())
                .Take(missing)
                .ToListAsync(ct);

            foreach (var q in extra)
            {
                takenQuestions.Add(q.Id);
                result.Add(MapToDto(q, language));
            }
        }

        result = result.Take(request.TotalQuestions).ToList();

        result = result
            .OrderBy(r => r.Type)
            .ThenBy(_ => Guid.NewGuid())
            .Select((q, idx) => q with { OrderIndex = idx + 1 })
            .ToList();

        return result;
    }

    private InterviewQuestionDto MapToDto(Question q, Technology? language)
    {
        int? timeLimit = null;
        int? memoryLimit = null;
        string? languageCode = null;

        if (q.Type == QuestionType.Coding && language != null)
        {
            languageCode = language.Code;
            
            var limit = q.LanguageLimits.FirstOrDefault(l => l.LanguageId == language.Id);
            if (limit != null)
            {
                timeLimit = limit.TimeLimitMs;
                memoryLimit = limit.MemoryLimitMb;
            }
        }

        return new InterviewQuestionDto
        {
            Text = q.Text,
            Type = q.Type,
            ReferenceSolution = q.ReferenceSolution,
            ProgrammingLanguageCode = languageCode,
            TimeLimitMs = timeLimit,
            MemoryLimitMb = memoryLimit,
            TestCases = q.TestCases
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new TestCaseDto
                {
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsHidden = tc.IsHidden
                }).ToList()
        };
    }
}

public record InterviewQuestionDto
{
    public string Text { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public int OrderIndex { get; init; }
    public string ReferenceSolution { get; init; } = string.Empty;
    public string? ProgrammingLanguageCode { get; init; }
    public int? TimeLimitMs { get; init; }
    public int? MemoryLimitMb { get; init; }
    public List<TestCaseDto> TestCases { get; init; } = [];
}

public record TestCaseDto
{
    public string Input { get; init; } = string.Empty;
    public string ExpectedOutput { get; init; } = string.Empty;
    public bool IsHidden { get; init; }
}
