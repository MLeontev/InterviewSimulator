using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.IntegrationTests.Infrastructure;
using Backend.IntegrationTests.Infrastructure.Fakes;
using FluentAssertions;
using Framework.Infrastructure.Outbox;
using Interview.UseCases.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CodeExecutionCompletedEvent = CodeExecution.IntegrationEvents.CodeSubmissionCompleted;
using CodeExecutionTestCaseResultDto = CodeExecution.IntegrationEvents.TestCaseResultDto;
using CodeExecutionVerdict = CodeExecution.IntegrationEvents.Verdict;
using InterviewAppDbContext = Interview.Infrastructure.Implementation.DataAccess.AppDbContext;
using InterviewCodeCheckTestCaseResult = Interview.Domain.Models.CodeCheckTestCaseResult;
using InterviewCodeSubmissionCreated = Interview.IntegrationEvents.CodeSubmissionCreated;
using InterviewQuestion = Interview.Domain.Entities.InterviewQuestion;
using InterviewQuestionStatus = Interview.Domain.Enums.QuestionStatus;
using InterviewQuestionType = Interview.Domain.Enums.QuestionType;
using InterviewSession = Interview.Domain.Entities.InterviewSession;
using InterviewStatus = Interview.Domain.Enums.InterviewStatus;
using InterviewTestCase = Interview.Domain.Entities.TestCase;
using InterviewVerdict = Interview.Domain.Enums.Verdict;

namespace Backend.IntegrationTests.Interview;

public abstract class InterviewIntegrationTestBase : BaseIntegrationTest
{
    protected static readonly JsonSerializerOptions ApiJsonOptions = CreateApiJsonOptions();

    protected InterviewIntegrationTestBase(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    protected async Task<Guid> CreateSessionAsync(AuthorizedUserContext userContext)
    {
        using var response = await userContext.Client.PostAsJsonAsync(
            "/api/v1/interview-session",
            new CreateSessionRequest(TestData.PythonMiddlePresetId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var session = await GetActiveSessionAsync(userContext.UserId);
        session.Should().NotBeNull();

        return session.Id;
    }

    protected FakeAiEvaluationService ResetFakeAiEvaluationService()
    {
        var fakeAi = Factory.GetFakeAiEvaluationService();
        fakeAi.Reset();
        return fakeAi;
    }

    protected async Task<InterviewSession?> GetActiveSessionAsync(Guid candidateId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        return await db.InterviewSessions
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .SingleOrDefaultAsync(x => x.CandidateId == candidateId && x.Status == InterviewStatus.InProgress);
    }

    protected async Task<InterviewSession?> GetSessionAsync(Guid sessionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        return await db.InterviewSessions
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .SingleOrDefaultAsync(x => x.Id == sessionId);
    }

    protected async Task<InterviewQuestion?> GetCurrentQuestionEntityAsync(Guid sessionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        return await db.InterviewQuestions
            .Include(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .Where(x => x.InterviewSessionId == sessionId)
            .Where(x =>
                x.Status != InterviewQuestionStatus.Skipped &&
                x.Status != InterviewQuestionStatus.Submitted &&
                x.Status != InterviewQuestionStatus.EvaluatingAi &&
                x.Status != InterviewQuestionStatus.EvaluatedAi &&
                x.Status != InterviewQuestionStatus.AiEvaluationFailed)
            .OrderBy(x => x.OrderIndex)
            .FirstOrDefaultAsync();
    }

    protected async Task<InterviewQuestion?> GetQuestionAsync(Guid questionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        return await db.InterviewQuestions
            .Include(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .SingleOrDefaultAsync(x => x.Id == questionId);
    }

    protected async Task<InterviewQuestion> MakeCodingQuestionCurrentAsync(Guid sessionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        var questions = await db.InterviewQuestions
            .Include(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .Where(x => x.InterviewSessionId == sessionId)
            .OrderBy(x => x.OrderIndex)
            .ToListAsync();

        var codingQuestion = questions.Single(x => x.Type == InterviewQuestionType.Coding);

        foreach (var theoryQuestion in questions.Where(x => x.Type == InterviewQuestionType.Theory))
        {
            var skipResult = theoryQuestion.Skip();
            skipResult.IsSuccess.Should().BeTrue();
        }

        await db.SaveChangesAsync();
        return codingQuestion;
    }

    protected async Task LeaveOnlyFirstQuestionActiveAsync(Guid sessionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        var questions = await db.InterviewQuestions
            .Where(x => x.InterviewSessionId == sessionId)
            .OrderBy(x => x.OrderIndex)
            .ToListAsync();

        foreach (var question in questions.Skip(1))
        {
            var skipResult = question.Skip();
            skipResult.IsSuccess.Should().BeTrue();
        }

        await db.SaveChangesAsync();
    }

    protected async Task<(Guid QuestionId, Guid SubmissionId)> PutCodingQuestionIntoEvaluatingCodeAsync(
        Guid sessionId,
        string code = "print('ok')")
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        var question = await db.InterviewQuestions
            .Include(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .SingleAsync(x => x.InterviewSessionId == sessionId && x.Type == InterviewQuestionType.Coding);

        var nowUtc = DateTime.UtcNow;
        var startResult = question.Start(nowUtc);
        startResult.IsSuccess.Should().BeTrue();

        var submissionId = Guid.NewGuid();
        var submitResult = question.SubmitDraftCode(code, submissionId, nowUtc);
        submitResult.IsSuccess.Should().BeTrue();

        await db.SaveChangesAsync();

        return (question.Id, submissionId);
    }

    protected async Task<(Guid QuestionId, Guid SubmissionId)> PrepareCodingQuestionEvaluatedAsync(
        Guid sessionId,
        string code = "print('ok')")
    {
        var (questionId, submissionId) = await PutCodingQuestionIntoEvaluatingCodeAsync(sessionId, code);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        var question = await db.InterviewQuestions
            .Include(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .SingleAsync(x => x.Id == questionId);

        var results = question.TestCases
            .OrderBy(x => x.OrderIndex)
            .Select(x => new InterviewCodeCheckTestCaseResult(
                InterviewTestCaseId: x.Id,
                ActualOutput: x.ExpectedOutput,
                ErrorMessage: null,
                TimeElapsedMs: 10,
                MemoryUsedMb: 2,
                Verdict: InterviewVerdict.OK))
            .ToList();

        var applyResult = question.ApplyCodeSubmissionResult(
            submissionId,
            results,
            InterviewVerdict.OK,
            DateTime.UtcNow,
            errorMessage: null);

        applyResult.IsSuccess.Should().BeTrue();
        applyResult.Value.Should().BeTrue();

        await db.SaveChangesAsync();

        return (questionId, submissionId);
    }

    protected async Task<OutboxMessage?> FindCodeSubmissionCreatedOutboxMessageAsync(Guid submissionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();
        var eventType = typeof(InterviewCodeSubmissionCreated).AssemblyQualifiedName;

        var messages = await db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Type == eventType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        foreach (var message in messages)
        {
            var payload = DeserializeCodeSubmissionCreated(message);
            if (payload?.SubmissionId == submissionId)
                return message;
        }

        return null;
    }

    protected static InterviewCodeSubmissionCreated? DeserializeCodeSubmissionCreated(OutboxMessage? message)
    {
        if (message is null)
            return null;

        return JsonSerializer.Deserialize<InterviewCodeSubmissionCreated>(message.Payload);
    }

    protected async Task ClearInterviewStateAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM "Interview"."TestCases";
            DELETE FROM "Interview"."InterviewQuestions";
            DELETE FROM "Interview"."InterviewSessions";
            DELETE FROM "Interview"."OutboxMessages";
            """);
    }

    protected async Task SetQuestionStatusAsync(Guid questionId, InterviewQuestionStatus status)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Interview"."InterviewQuestions" SET "Status" = {status.ToString()} WHERE "Id" = {questionId}""");
    }

    protected async Task SetSessionStatusAsync(Guid sessionId, InterviewStatus status)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Interview"."InterviewSessions" SET "Status" = {status.ToString()} WHERE "Id" = {sessionId}""");
    }

    protected async Task SetSessionPlannedEndAtAsync(Guid sessionId, DateTime plannedEndAtUtc)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Interview"."InterviewSessions" SET "PlannedEndAt" = {plannedEndAtUtc} WHERE "Id" = {sessionId}""");
    }

    protected async Task MarkQuestionAiEvaluationFailedAsync(
        Guid questionId,
        int retryCount = 3,
        string errorMessage = "AI-оценка недоступна после нескольких попыток.")
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();
        var nowUtc = DateTime.UtcNow;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
              UPDATE "Interview"."InterviewQuestions"
              SET "Status" = {nameof(InterviewQuestionStatus.AiEvaluationFailed)},
                  "AiRetryCount" = {retryCount},
                  "AiNextRetryAt" = {(DateTime?)null},
                  "EvaluatedAt" = {nowUtc},
                  "ErrorMessage" = {errorMessage}
              WHERE "Id" = {questionId}
              """);
    }

    protected async Task MarkSessionAiEvaluationFailedAsync(
        Guid sessionId,
        int retryCount = 2,
        string aiFeedbackJson = """{"summary":"Temporary failure"}""")
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
              UPDATE "Interview"."InterviewSessions"
              SET "Status" = {nameof(InterviewStatus.AiEvaluationFailed)},
                  "AiRetryCount" = {retryCount},
                  "AiNextRetryAt" = {(DateTime?)null},
                  "AiFeedbackJson" = CAST({aiFeedbackJson} AS jsonb)
              WHERE "Id" = {sessionId}
              """);
    }

    protected async Task SetSessionFinishedAtAsync(Guid sessionId, DateTime finishedAtUtc)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterviewAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Interview"."InterviewSessions" SET "FinishedAt" = {finishedAtUtc} WHERE "Id" = {sessionId}""");
    }

    protected async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        int attempts = 30,
        int delayMs = 100)
    {
        for (var i = 0; i < attempts; i++)
        {
            if (await condition())
                return;

            await Task.Delay(delayMs);
        }

        throw new TimeoutException("Condition was not met in time.");
    }

    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(request);
    }

    protected async Task EvaluateTheoryQuestionAsync(Guid questionId)
    {
        await SetQuestionStatusAsync(questionId, InterviewQuestionStatus.EvaluatingAi);

        var result = await SendAsync(new EvaluateTheoryAnswerCommand(questionId));
        result.IsSuccess.Should().BeTrue();
    }

    protected async Task EvaluateCodingQuestionAsync(Guid questionId)
    {
        await SetQuestionStatusAsync(questionId, InterviewQuestionStatus.EvaluatingAi);

        var result = await SendAsync(new EvaluateCodingAnswerCommand(questionId));
        result.IsSuccess.Should().BeTrue();
    }

    protected async Task EvaluateSessionAsync(Guid sessionId)
    {
        await SetSessionStatusAsync(sessionId, InterviewStatus.EvaluatingAi);

        var result = await SendAsync(new EvaluateInterviewSessionCommand(sessionId));
        result.IsSuccess.Should().BeTrue();
    }

    protected static CodeExecutionCompletedEvent BuildCompletedEvent(
        Guid submissionId,
        Guid interviewQuestionId,
        IReadOnlyList<InterviewTestCase> testCases,
        CodeExecutionVerdict overallVerdict,
        string? errorMessage = null)
    {
        var firstOrderIndex = testCases.Min(x => x.OrderIndex);

        var testCaseResults = testCases
            .OrderBy(x => x.OrderIndex)
            .Select(x => new CodeExecutionTestCaseResultDto(
                InterviewTestCaseId: x.Id,
                OrderIndex: x.OrderIndex,
                Input: x.Input,
                ExpectedOutput: x.ExpectedOutput,
                ActualOutput: x.ExpectedOutput,
                Error: string.Empty,
                ExitCode: 0,
                TimeElapsedMs: 11,
                MemoryUsedMb: 2,
                Verdict: overallVerdict == CodeExecutionVerdict.OK
                    ? CodeExecutionVerdict.OK
                    : x.OrderIndex == firstOrderIndex
                        ? overallVerdict
                        : CodeExecutionVerdict.OK))
            .ToList();

        var passedCount = testCaseResults.Count(x => x.Verdict == CodeExecutionVerdict.OK);

        return new CodeExecutionCompletedEvent(
            SubmissionId: submissionId,
            InterviewQuestionId: interviewQuestionId,
            TestCaseResults: testCaseResults,
            OverallVerdict: overallVerdict,
            PassedCount: passedCount,
            TotalTests: testCases.Count,
            ErrorMessage: errorMessage);
    }

    private static JsonSerializerOptions CreateApiJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record CreateSessionRequest(Guid InterviewPresetId);
}
