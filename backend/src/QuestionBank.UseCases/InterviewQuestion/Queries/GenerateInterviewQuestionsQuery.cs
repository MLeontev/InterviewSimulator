using Framework.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.UseCases.InterviewQuestion.Queries;

public record GenerateInterviewQuestionsQuery(Guid PresetId, int TheoryCount, int CodingCount) : IRequest<Result<GeneratedQuestionSetDto>>;

internal class GenerateInterviewQuestionsHandler(IDbContext dbContext) : IRequestHandler<GenerateInterviewQuestionsQuery, Result<GeneratedQuestionSetDto>>
{
    public async Task<Result<GeneratedQuestionSetDto>> Handle(GenerateInterviewQuestionsQuery request, CancellationToken cancellationToken)
    {
        if (request.TheoryCount < 0 || request.CodingCount < 0)
        {
            var errors = new Dictionary<string, string[]>();
            if (request.TheoryCount < 0)
                errors[nameof(request.TheoryCount)] = ["TheoryCount должен быть больше или равен 0."];
            if (request.CodingCount < 0)
                errors[nameof(request.CodingCount)] = ["CodingCount должен быть больше или равен 0."];

            return Result.Failure<GeneratedQuestionSetDto>(Error.Validation(errors));
        }

        var preset = await dbContext.InterviewPresets
            .AsNoTracking()
            .Include(x => x.Technologies)
            .ThenInclude(x => x.Technology)
            .Include(x => x.InterviewPresetCompetencies)
            .FirstOrDefaultAsync(x => x.Id == request.PresetId, cancellationToken);
        
        if (preset is null)
            return Result.Failure<GeneratedQuestionSetDto>(Error.NotFound("PRESET_NOT_FOUND", "Пресет интервью не найден"));
        
        var competencyWeights = preset.InterviewPresetCompetencies
            .ToDictionary(x => x.CompetencyId, x => x.Weight);
        
        if (competencyWeights.Count == 0)
            return Result.Failure<GeneratedQuestionSetDto>(Error.Business("PRESET_COMPETENCIES_NOT_CONFIGURED", "Для пресета не настроены компетенции"));
            
        var presetTechnologyIds = preset.Technologies
            .Select(x => x.TechnologyId)
            .ToHashSet();
        
        if (presetTechnologyIds.Count == 0)
            return Result.Failure<GeneratedQuestionSetDto>(Error.Business("PRESET_TECHNOLOGIES_NOT_CONFIGURED", "Для пресета не настроен стек технологий"));

        var programmingLanguage = preset.Technologies
            .FirstOrDefault(x => x.Technology.Category == TechnologyCategory.ProgrammingLanguage);
        
        if (programmingLanguage is null)
            return Result.Failure<GeneratedQuestionSetDto>(Error.Business("PRESET_PROGRAMMING_LANGUAGE_NOT_CONFIGURED", "Для пресета не настроен язык программирования"));

        var programmingLanguageId = programmingLanguage.TechnologyId;
        var programmingLanguageCode = programmingLanguage.Technology.Code;
        
        var competencyIds = competencyWeights.Keys.ToHashSet();
        
        var theoryPool = await dbContext.Questions
            .AsNoTracking()
            .Include(x => x.Competency)
            .Where(x => 
                x.Type == QuestionType.Theory && 
                x.GradeId == preset.GradeId &&
                competencyIds.Contains(x.CompetencyId) &&
                (x.TechnologyId == null || presetTechnologyIds.Contains(x.TechnologyId.Value)))
            .ToListAsync(cancellationToken);
        
        var codingPool = await dbContext.Questions
            .AsNoTracking()
            .Include(x => x.Competency)
            .Include(x => x.LanguageLimits)
            .ThenInclude(x => x.Language)
            .Include(x => x.TestCases)
            .Where(x =>
                x.Type == QuestionType.Coding &&
                x.GradeId == preset.GradeId &&
                competencyIds.Contains(x.CompetencyId) &&
                x.LanguageLimits.Any(l => l.LanguageId == programmingLanguageId))
            .ToListAsync(cancellationToken);
        
        var theoryPlan = BuildQuotaPlan(
            competencyWeights,
            request.TheoryCount,
            theoryPool.Select(x => x.CompetencyId));

        var codingPlan = BuildQuotaPlan(
            competencyWeights,
            request.CodingCount,
            codingPool.Select(x => x.CompetencyId));
        
        var selectedTheory = PickTheoryQuestions(theoryPool, theoryPlan);
        selectedTheory = FillRemainingTheoryQuestions(selectedTheory, theoryPool, request.TheoryCount);
        
        var selectedCoding = PickCodingQuestions(codingPool, codingPlan, programmingLanguageId, programmingLanguageCode);
        selectedCoding = FillRemainingCodingQuestions(selectedCoding, codingPool, request.CodingCount, programmingLanguageId, programmingLanguageCode);
        
        var orderedQuestions = selectedTheory
            .Concat(selectedCoding)
            .Select((q, index) => q with { OrderIndex = index + 1 })
            .ToList();
        
        return Result.Success(new GeneratedQuestionSetDto(preset.Id, orderedQuestions));
    }

    private IReadOnlyDictionary<Guid, int> BuildQuotaPlan(
        IReadOnlyDictionary<Guid, double> weights, 
        int totalCount, 
        IEnumerable<Guid> availableCompetencyIds)
    {
        if (totalCount == 0)
            return new Dictionary<Guid, int>();
        
        var availableSet = availableCompetencyIds.ToHashSet();
        
        var filteredWeights = weights
            .Where(x => availableSet.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);
        
        if (filteredWeights.Count == 0)
            return new Dictionary<Guid, int>();
        
        var weightSum = filteredWeights.Values.Sum();

        var raw = filteredWeights
            .Select(x => new
            {
                CompetencyId = x.Key,
                Exact = x.Value / weightSum * totalCount
            })
            .ToList();
        
        var result = raw.ToDictionary(x => x.CompetencyId, x => (int)Math.Floor(x.Exact));
        var assigned = result.Values.Sum();
        var remaining = totalCount - assigned;

        foreach (var item in raw
                     .OrderByDescending(x => x.Exact - Math.Floor(x.Exact))
                     .ThenBy(x => x.CompetencyId)
                     .Take(remaining))
        {
            result[item.CompetencyId]++;
        }
        
        return result;
    }
    
    private List<GeneratedQuestionDto> PickTheoryQuestions(
        IReadOnlyCollection<Question> pool, 
        IReadOnlyDictionary<Guid, int> plan)
    {
        var selected = new List<GeneratedQuestionDto>();
        var usedQuestionIds = new HashSet<Guid>();
        var rng = Random.Shared;

        foreach (var (competencyId, count) in plan)
        {
            if (count <= 0)
                continue;
            
            var candidates = pool
                .Where(x => x.CompetencyId == competencyId && !usedQuestionIds.Contains(x.Id))
                .OrderBy(_ => rng.Next())
                .Take(count)
                .ToList();

            foreach (var question in candidates)
            {
                usedQuestionIds.Add(question.Id);
                
                selected.Add(new GeneratedQuestionDto(
                    QuestionId: question.Id,
                    OrderIndex: 0,
                    Type: QuestionType.Theory,
                    Title: question.Title,
                    Text: question.Text,
                    ReferenceSolution: question.ReferenceSolution,
                    CompetencyId: question.CompetencyId,
                    CompetencyName: question.Competency.Name,
                    ProgrammingLanguageCode: null,
                    TimeLimitMs: null,
                    MemoryLimitMb: null,
                    TestCases: []));
            }
        }
        
        return selected;
    }

    private List<GeneratedQuestionDto> PickCodingQuestions(
        IReadOnlyCollection<Question> pool,
        IReadOnlyDictionary<Guid, int> plan,
        Guid programmingLanguageId,
        string programmingLanguageCode)
    {
        var selected = new List<GeneratedQuestionDto>();
        var usedQuestionIds = new HashSet<Guid>();
        var rng = Random.Shared;

        foreach (var (competencyId, count) in plan)
        {
            if (count <= 0)
                continue;

            var candidates = pool
                .Where(x => x.CompetencyId == competencyId && !usedQuestionIds.Contains(x.Id))
                .OrderBy(_ => rng.Next())
                .Take(count)
                .ToList();

            foreach (var question in candidates)
            {
                usedQuestionIds.Add(question.Id);
                
                var limit = question.LanguageLimits.First(x => x.LanguageId == programmingLanguageId);
                
                var testCases = question.TestCases
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => new GeneratedTestCaseDto(
                        Input: x.Input,
                        ExpectedOutput: x.ExpectedOutput,
                        IsHidden: x.IsHidden,
                        OrderIndex: x.OrderIndex))
                    .ToList();
                
                selected.Add(new GeneratedQuestionDto(
                    QuestionId: question.Id,
                    OrderIndex: 0,
                    Type: QuestionType.Coding,
                    Title: question.Title,
                    Text: question.Text,
                    ReferenceSolution: question.ReferenceSolution,
                    CompetencyId: question.CompetencyId,
                    CompetencyName: question.Competency.Name,
                    ProgrammingLanguageCode: programmingLanguageCode,
                    TimeLimitMs: limit.TimeLimitMs,
                    MemoryLimitMb: limit.MemoryLimitMb,
                    TestCases: testCases));
            }
        }
        
        return selected;
    }
    
    private List<GeneratedQuestionDto> FillRemainingTheoryQuestions(
        List<GeneratedQuestionDto> selected,
        IReadOnlyCollection<Question> pool,
        int requiredCount)
    {
        if (selected.Count >= requiredCount)
            return selected;

        var usedQuestionIds = selected
            .Select(x => x.QuestionId)
            .ToHashSet();

        var rng = Random.Shared;

        var additional = pool
            .Where(x => !usedQuestionIds.Contains(x.Id))
            .OrderBy(_ => rng.Next())
            .Take(requiredCount - selected.Count)
            .Select(question => new GeneratedQuestionDto(
                QuestionId: question.Id,
                OrderIndex: 0,
                Type: QuestionType.Theory,
                Title: question.Title,
                Text: question.Text,
                ReferenceSolution: question.ReferenceSolution,
                CompetencyId: question.CompetencyId,
                CompetencyName: question.Competency.Name,
                ProgrammingLanguageCode: null,
                TimeLimitMs: null,
                MemoryLimitMb: null,
                TestCases: []))
            .ToList();

        selected.AddRange(additional);
        return selected;
    }
    
    private List<GeneratedQuestionDto> FillRemainingCodingQuestions(
        List<GeneratedQuestionDto> selected,
        IReadOnlyCollection<Question> pool,
        int requiredCount,
        Guid programmingLanguageId,
        string programmingLanguageCode)
    {
        if (selected.Count >= requiredCount)
            return selected;

        var usedQuestionIds = selected
            .Select(x => x.QuestionId)
            .ToHashSet();

        var rng = Random.Shared;

        var additionalQuestions = pool
            .Where(x => !usedQuestionIds.Contains(x.Id))
            .OrderBy(_ => rng.Next())
            .Take(requiredCount - selected.Count)
            .ToList();

        var additional = additionalQuestions
            .Select(question =>
            {
                var limit = question.LanguageLimits.First(x => x.LanguageId == programmingLanguageId);

                var testCases = question.TestCases
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => new GeneratedTestCaseDto(
                        Input: x.Input,
                        ExpectedOutput: x.ExpectedOutput,
                        IsHidden: x.IsHidden,
                        OrderIndex: x.OrderIndex))
                    .ToList();

                return new GeneratedQuestionDto(
                    QuestionId: question.Id,
                    OrderIndex: 0,
                    Type: QuestionType.Coding,
                    Title: question.Title,
                    Text: question.Text,
                    ReferenceSolution: question.ReferenceSolution,
                    CompetencyId: question.CompetencyId,
                    CompetencyName: question.Competency.Name,
                    ProgrammingLanguageCode: programmingLanguageCode,
                    TimeLimitMs: limit.TimeLimitMs,
                    MemoryLimitMb: limit.MemoryLimitMb,
                    TestCases: testCases);
            })
            .ToList();

        selected.AddRange(additional);
        return selected;
    }
}

public record GeneratedQuestionSetDto(
    Guid PresetId,
    IReadOnlyList<GeneratedQuestionDto> Questions);

public record GeneratedQuestionDto(
    Guid QuestionId,
    int OrderIndex,
    QuestionType Type,
    string Title,
    string Text,
    string ReferenceSolution,
    Guid CompetencyId,
    string CompetencyName,
    string? ProgrammingLanguageCode,
    int? TimeLimitMs,
    int? MemoryLimitMb,
    IReadOnlyList<GeneratedTestCaseDto> TestCases);

public record GeneratedTestCaseDto(
    string Input,
    string ExpectedOutput,
    bool IsHidden,
    int OrderIndex);
