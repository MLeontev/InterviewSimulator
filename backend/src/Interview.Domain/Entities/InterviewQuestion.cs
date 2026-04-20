using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Domain.Models;

namespace Interview.Domain.Entities;

public class InterviewQuestion
{
    private readonly List<TestCase> _testCases = [];
    public IReadOnlyList<TestCase> TestCases => _testCases;
    
    public Guid Id { get; private set; }
    
    public Guid InterviewSessionId { get; private set; }
    public InterviewSession InterviewSession { get; private set; } = null!;
    
    public string Title { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public QuestionType Type { get; private set; }
    public Guid? CompetencyId { get; private set; }
    public string? CompetencyName { get; private set; }
    
    public int OrderIndex { get; private set; }
    
    public DateTime? StartedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? EvaluatedAt { get; private set; }
    public QuestionStatus Status { get; private set; } = QuestionStatus.NotStarted;
    
    public string? Answer { get; private set; }
    public string ReferenceSolution { get; private set; } = string.Empty;
    public string? AiFeedbackJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    public int AiRetryCount { get; private set; }
    public DateTime? AiNextRetryAt { get; private set; }
    
    public Guid? LastSubmissionId { get; private set; }
    public string? ProgrammingLanguageCode { get; private set; }
    public QuestionVerdict QuestionVerdict { get; private set; } = QuestionVerdict.None;
    public Verdict OverallVerdict { get; private set; } = Verdict.None;
    public int? TimeLimitMs { get; private set; }
    public int? MemoryLimitMb { get; private set; }

    public static InterviewQuestion Create(
        Guid sessionId,
        string title,
        string text,
        QuestionType type,
        int orderIndex,
        string referenceSolution,
        Guid? competencyId,
        string? competencyName,
        string? programmingLanguageCode,
        int? timeLimitMs,
        int? memoryLimitMb,
        IReadOnlyCollection<TestCase>? testCases = null)
    {
        var questionId = Guid.NewGuid();

        var question = new InterviewQuestion
        {
            Id = questionId,
            InterviewSessionId = sessionId,
            Title = title,
            Text = text,
            Type = type,
            OrderIndex = orderIndex,
            ReferenceSolution = referenceSolution,
            CompetencyId = competencyId,
            CompetencyName = competencyName,
            ProgrammingLanguageCode = programmingLanguageCode,
            Status = QuestionStatus.NotStarted,
            OverallVerdict = Verdict.None,
            TimeLimitMs = timeLimitMs,
            MemoryLimitMb = memoryLimitMb
        };
        
        if (testCases is not null)
        {
            foreach (var testCase in testCases.OrderBy(tc => tc.OrderIndex))
            {
                testCase.AttachToQuestion(questionId);
                question._testCases.Add(testCase);
            }
        }

        return question;
    }

    public Result Start(DateTime nowUtc)
    {
        if (Status == QuestionStatus.InProgress)
            return Result.Success();

        if (Status != QuestionStatus.NotStarted)
        {
            return Result.Failure(Error.Business(
                "QUESTION_CANNOT_BE_STARTED", 
                "Это задание нельзя запустить в текущем статусе"));
        }

        Status = QuestionStatus.InProgress;
        StartedAt = nowUtc;
        
        return Result.Success();
    }

    public Result Skip()
    {
        if (Status is not (
            QuestionStatus.NotStarted
            or QuestionStatus.InProgress
            or QuestionStatus.EvaluatedCode))
        {
            return Result.Failure(Error.Business(
                "QUESTION_CANNOT_BE_SKIPPED", 
                "Это задание сейчас нельзя пропустить"));
        }
        
        Status = QuestionStatus.Skipped;
        Answer = null;
        QuestionVerdict = QuestionVerdict.None;
        OverallVerdict = Verdict.None;
        AiFeedbackJson = null;
        ErrorMessage = null;
        SubmittedAt = null;
        EvaluatedAt = null;
        
        foreach (var testCase in TestCases)
            testCase.Reset();

        return Result.Success();
    }

    public Result SubmitTheoryAnswer(string answer, DateTime nowUtc)
    {
        if (Type != QuestionType.Theory)
            return Result.Failure(Error.Business(
                "QUESTION_NOT_THEORY",
                "Задание не является теоретическим вопросом"));

        if (Status != QuestionStatus.InProgress)
        {
            return Result.Failure(Error.Business(
                "QUESTION_NOT_IN_PROGRESS",
                "Теоретический ответ можно отправить только для начатого задания"));
        }

        Answer = answer.Trim();
        SubmittedAt = nowUtc;
        EvaluatedAt = null;
        AiFeedbackJson = null;
        ErrorMessage = null;
        QuestionVerdict = QuestionVerdict.None;
        Status = QuestionStatus.Submitted;
        AiRetryCount = 0;
        AiNextRetryAt = null;

        return Result.Success();
    }
    
    public Result SubmitDraftCode(string code, Guid submissionId, DateTime nowUtc)
    {
        if (Type != QuestionType.Coding)
        {
            return Result.Failure(Error.Business(
                "QUESTION_NOT_CODING",
                "Задание не является задачей на написание кода"));
        }

        if (string.IsNullOrWhiteSpace(ProgrammingLanguageCode))
        {
            return Result.Failure(Error.Business(
                "LANGUAGE_NOT_SET",
                "Для задания не задан язык программирования"));
        }

        if (Status == QuestionStatus.EvaluatingCode)
        {
            return Result.Failure(Error.Business(
                "CODE_CHECK_IN_PROGRESS",
                "Проверка кода уже выполняется. Дождитесь результата текущего запуска"));
        }

        if (Status is not (QuestionStatus.InProgress or QuestionStatus.EvaluatedCode))
        {
            return Result.Failure(Error.Business(
                "QUESTION_NOT_READY_FOR_DRAFT_SUBMIT",
                "Черновую отправку можно выполнить только для начатого задания или после предыдущей проверки"));
        }

        Answer = code;
        Status = QuestionStatus.EvaluatingCode;
        SubmittedAt = nowUtc;
        EvaluatedAt = null;
        AiFeedbackJson = null;
        ErrorMessage = null;
        QuestionVerdict = QuestionVerdict.None;
        OverallVerdict = Verdict.None;
        LastSubmissionId = submissionId;

        foreach (var testCase in TestCases)
            testCase.Reset();

        return Result.Success();
    }
    
    public Result SubmitCode(DateTime nowUtc)
    {
        if (Type != QuestionType.Coding)
        {
            return Result.Failure(Error.Business(
                "QUESTION_INVALID_TYPE",
                "Задание не является задачей на написание кода"));
        }

        if (Status != QuestionStatus.EvaluatedCode)
        {
            return Result.Failure(Error.Business(
                "CODE_NOT_EVALUATED",
                "Сначала дождитесь результата проверки кода на тестах"));
        }

        Status = QuestionStatus.Submitted;
        SubmittedAt = nowUtc;
        AiRetryCount = 0;
        AiNextRetryAt = null;

        return Result.Success();
    }

    public Result<bool> ApplyCodeSubmissionResult(
        Guid submissionId,
        IReadOnlyCollection<CodeCheckTestCaseResult> testCaseResults,
        Verdict overallVerdict,
        DateTime nowUtc,
        string? errorMessage)
    {
        if (Type != QuestionType.Coding)
        {
            return Result.Failure<bool>(Error.Business(
                "QUESTION_NOT_CODING",
                "Задание не является задачей на написание кода"));
        }

        if (LastSubmissionId != submissionId || Status != QuestionStatus.EvaluatingCode)
            return Result.Success(false);
        
        OverallVerdict = overallVerdict;
        EvaluatedAt = nowUtc;
        Status = QuestionStatus.EvaluatedCode;
        QuestionVerdict = ResolveDraftCodeQuestionVerdict(testCaseResults);
        
        ErrorMessage = overallVerdict == Verdict.FailedSystem
            ? string.IsNullOrWhiteSpace(errorMessage)
                ? "Системная ошибка проверки кода"
                : errorMessage
            : null;
        
        var testCasesById = TestCases.ToDictionary(tc => tc.Id);
        foreach (var result in testCaseResults)
        {
            if (!testCasesById.TryGetValue(result.InterviewTestCaseId, out var testCase))
                continue;

            testCase.ApplyExecutionResult(
                result.ActualOutput,
                result.TimeElapsedMs,
                result.MemoryUsedMb,
                result.Verdict,
                result.ErrorMessage);
        }

        return Result.Success(true);
    }
    
    public Result ApplyAiEvaluationSuccess(string rawJson, int score, DateTime nowUtc)
    {
        if (Status != QuestionStatus.EvaluatingAi)
        {
            return Result.Failure(Error.Business(
                "QUESTION_NOT_IN_EVALUATING_AI",
                "Задание не готово к AI-оценке"));
        }

        AiFeedbackJson = rawJson;
        EvaluatedAt = nowUtc;
        QuestionVerdict = ResolveAiQuestionVerdict(score);
        Status = QuestionStatus.EvaluatedAi;
        ErrorMessage = null;
        AiRetryCount = 0;
        AiNextRetryAt = null;

        return Result.Success();
    }
    
    public Result ScheduleAiEvaluationRetry(int nextRetryCount, DateTime nextRetryAtUtc, int maxRetries)
    {
        if (Status != QuestionStatus.EvaluatingAi)
        {
            return Result.Failure(Error.Business(
                "QUESTION_NOT_IN_EVALUATING_AI",
                "Задание не готово к AI-оценке"));
        }

        AiRetryCount = nextRetryCount;
        AiNextRetryAt = nextRetryAtUtc;
        Status = QuestionStatus.Submitted;
        ErrorMessage = $"AI временно недоступен. Повтор {nextRetryCount}/{maxRetries}.";

        return Result.Success();
    }
    
    public Result MarkAiEvaluationFailed(int retryCount, DateTime nowUtc)
    {
        if (Status != QuestionStatus.EvaluatingAi)
        {
            return Result.Failure(Error.Business(
                "QUESTION_NOT_IN_EVALUATING_AI",
                "Задание не готово к AI-оценке"));
        }

        AiRetryCount = retryCount;
        Status = QuestionStatus.AiEvaluationFailed;
        EvaluatedAt = nowUtc;
        AiNextRetryAt = null;
        ErrorMessage = "AI-оценка недоступна после нескольких попыток.";

        return Result.Success();
    }

    public Result<bool> ResetForAiRetry(DateTime nowUtc)
    {
        if (Status != QuestionStatus.AiEvaluationFailed)
            return Result.Success(false);

        if (string.IsNullOrWhiteSpace(Answer))
            return Result.Success(false);

        Status = QuestionStatus.Submitted;
        SubmittedAt = nowUtc;
        EvaluatedAt = null;
        AiRetryCount = 0;
        AiNextRetryAt = null;
        AiFeedbackJson = null;
        ErrorMessage = null;
        QuestionVerdict = QuestionVerdict.None;

        return Result.Success(true);
    }

    public void MarkSkippedWhenSessionFinishes()
    {
        if (Status is not (
            QuestionStatus.InProgress
            or QuestionStatus.EvaluatingCode
            or QuestionStatus.EvaluatedCode))
        {
            return;
        }

        Status = QuestionStatus.Skipped;
    }
    
    private static QuestionVerdict ResolveDraftCodeQuestionVerdict(
        IReadOnlyCollection<CodeCheckTestCaseResult> testCaseResults)
    {
        var total = testCaseResults.Count;
        var passed = testCaseResults.Count(x => x.Verdict == Verdict.OK);

        return total == 0
            ? QuestionVerdict.Incorrect
            : passed == total
                ? QuestionVerdict.Correct
                : passed > 0
                    ? QuestionVerdict.PartiallyCorrect
                    : QuestionVerdict.Incorrect;
    }
    
    private static QuestionVerdict ResolveAiQuestionVerdict(int score) =>
        score switch
        {
            <= 3 => QuestionVerdict.Incorrect,
            <= 6 => QuestionVerdict.PartiallyCorrect,
            _ => QuestionVerdict.Correct
        };
}
