using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using InterviewQuestionType = Interview.Domain.Enums.QuestionType;
using InterviewQuestionStatus = Interview.Domain.Enums.QuestionStatus;
using InterviewStatus = Interview.Domain.Enums.InterviewStatus;
using InterviewVerdict = Interview.Domain.Enums.Verdict;

namespace Backend.IntegrationTests.Interview;

public sealed class InterviewSessionApiTests : InterviewIntegrationTestBase
{
    public InterviewSessionApiTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateSession_ShouldCreateSessionAndPersistQuestions_WhenPresetExists()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        var session = await GetSessionAsync(sessionId);

        session.Should().NotBeNull();

        session.CandidateId.Should().Be(userContext.UserId);
        session.InterviewPresetId.Should().Be(TestData.PythonMiddlePresetId);
        session.InterviewPresetName.Should().Be(TestData.PythonMiddlePresetName);
        session.Status.Should().Be(InterviewStatus.InProgress);
        session.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        session.PlannedEndAt.Should().BeAfter(session.StartedAt);

        session.Questions.Should().HaveCount(4);
        session.Questions.Select(x => x.OrderIndex).Should().Equal(1, 2, 3, 4);
        session.Questions.Count(x => x.Type == InterviewQuestionType.Theory).Should().Be(3);
        session.Questions.Count(x => x.Type == InterviewQuestionType.Coding).Should().Be(1);

        var codingQuestion = session.Questions.Single(x => x.Type == InterviewQuestionType.Coding);
        codingQuestion.ProgrammingLanguageCode.Should().NotBeNullOrWhiteSpace();
        codingQuestion.TimeLimitMs.Should().BePositive();
        codingQuestion.MemoryLimitMb.Should().BePositive();
        codingQuestion.TestCases.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateSession_ShouldReturnConflict_WhenActiveSessionAlreadyExists()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        await CreateSessionAsync(userContext);

        using var response = await userContext.Client.PostAsJsonAsync(
            "/api/v1/interview-session",
            new CreateSessionRequest(TestData.PythonMiddlePresetId));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload.Code.Should().Be("ACTIVE_SESSION_EXISTS");
        payload.Description.Should().Be("У кандидата уже есть активная сессия интервью");
    }

    [Fact]
    public async Task GetCurrentSession_ShouldReturnSummary_WhenSessionExists()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);

        using (var startResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var submitResponse = await userContext.Client.PostAsJsonAsync(
                   "/api/v1/interview-session/question/submit-theory",
                   new SubmitTheoryRequest("  my theory answer  ")))
        {
            submitResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        using var response = await userContext.Client.GetAsync("/api/v1/interview-session");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CurrentInterviewSessionResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload.SessionId.Should().Be(sessionId);
        payload.Status.Should().Be(InterviewStatus.InProgress);
        payload.TotalQuestions.Should().Be(4);
        payload.AnsweredQuestions.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentQuestion_ShouldReturnCodingQuestionWithoutHiddenTestCases_WhenCodingQuestionIsCurrent()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        var codingQuestion = await MakeCodingQuestionCurrentAsync(sessionId);

        using var response = await userContext.Client.GetAsync("/api/v1/interview-session/question");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CurrentInterviewQuestionResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload.QuestionId.Should().Be(codingQuestion.Id);
        payload.Type.Should().Be(InterviewQuestionType.Coding);
        payload.Status.Should().Be(InterviewQuestionStatus.NotStarted);
        payload.ProgrammingLanguageCode.Should().NotBeNullOrWhiteSpace();

        var visibleTestCases = codingQuestion.TestCases
            .Where(x => !x.IsHidden)
            .OrderBy(x => x.OrderIndex)
            .ToList();

        codingQuestion.TestCases.Count.Should().BeGreaterThanOrEqualTo(visibleTestCases.Count);
        payload.TestCases.Should().HaveCount(visibleTestCases.Count);
        payload.TestCases.Select(x => x.OrderIndex).Should().Equal(visibleTestCases.Select(x => x.OrderIndex));
        payload.TestCases.Select(x => x.ExpectedOutput).Should().Equal(visibleTestCases.Select(x => x.ExpectedOutput));
    }

    [Fact]
    public async Task StartQuestion_ShouldMoveCurrentQuestionToInProgress()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);

        using var response = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var currentQuestion = await GetCurrentQuestionEntityAsync(sessionId);

        currentQuestion.Should().NotBeNull();
        currentQuestion.Status.Should().Be(InterviewQuestionStatus.InProgress);
        currentQuestion.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitTheoryAnswer_ShouldSaveTrimmedAnswerAndFinishSession_WhenItIsLastActiveQuestion()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await LeaveOnlyFirstQuestionActiveAsync(sessionId);

        using (var startResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await userContext.Client.PostAsJsonAsync(
            "/api/v1/interview-session/question/submit-theory",
            new SubmitTheoryRequest("  final theory answer  "));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var session = await GetSessionAsync(sessionId);

        session.Should().NotBeNull();
        session.Status.Should().Be(InterviewStatus.Finished);

        var firstQuestion = session.Questions.OrderBy(x => x.OrderIndex).First();
        firstQuestion.Status.Should().Be(InterviewQuestionStatus.Submitted);
        firstQuestion.Answer.Should().Be("final theory answer");
    }

    [Fact]
    public async Task SubmitTheoryAnswer_ShouldReturnBadRequest_WhenAnswerIsEmpty()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        await CreateSessionAsync(userContext);

        using (var startResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await userContext.Client.PostAsJsonAsync(
            "/api/v1/interview-session/question/submit-theory",
            new SubmitTheoryRequest("   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload!.Code.Should().Be("VALIDATION_ERROR");
        payload.Errors.Should().ContainKey("Answer");
        payload.Errors["Answer"].Should().Contain("Ответ не может быть пустым");
    }

    [Fact]
    public async Task SubmitDraftCodeAnswer_ShouldMoveQuestionToEvaluatingCode_AndWriteCodeSubmissionCreatedOutboxMessage()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await MakeCodingQuestionCurrentAsync(sessionId);

        using (var startResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        const string code = "print('hello')";

        using var response = await userContext.Client.PostAsJsonAsync(
            "/api/v1/interview-session/question/submit-draft-code",
            new SubmitDraftCodeRequest(code));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var session = await GetSessionAsync(sessionId);
        var codingQuestion = session!.Questions.Single(x => x.Type == InterviewQuestionType.Coding);
        var submissionId = codingQuestion.LastSubmissionId;
        var outboxMessage = await FindCodeSubmissionCreatedOutboxMessageAsync(submissionId!.Value);
        var payload = DeserializeCodeSubmissionCreated(outboxMessage);

        codingQuestion.Status.Should().Be(InterviewQuestionStatus.EvaluatingCode);
        codingQuestion.Answer.Should().Be(code);
        submissionId.Should().NotBeNull();

        outboxMessage.Should().NotBeNull();
        payload.Should().NotBeNull();

        payload.SubmissionId.Should().Be(submissionId.Value);
        payload.InterviewQuestionId.Should().Be(codingQuestion.Id);
        payload.Code.Should().Be(code);
        payload.LanguageCode.Should().Be(codingQuestion.ProgrammingLanguageCode);
        payload.TimeLimitMs.Should().Be(codingQuestion.TimeLimitMs);
        payload.MemoryLimitMb.Should().Be(codingQuestion.MemoryLimitMb);
        payload.TestCases.Select(x => x.OrderIndex).Should().Equal(codingQuestion.TestCases.Select(x => x.OrderIndex));
    }

    [Fact]
    public async Task SubmitDraftCodeAnswer_ShouldReturnBadRequest_WhenCodeIsEmpty()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await MakeCodingQuestionCurrentAsync(sessionId);

        using (var startResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await userContext.Client.PostAsJsonAsync(
            "/api/v1/interview-session/question/submit-draft-code",
            new SubmitDraftCodeRequest("   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload!.Code.Should().Be("VALIDATION_ERROR");
        payload.Errors.Should().ContainKey("Code");
        payload.Errors["Code"].Should().Contain("Код не может быть пустым");
    }

    [Fact]
    public async Task SubmitCodeAnswer_ShouldMarkCodingQuestionSubmittedAndFinishSession_WhenItIsLastActiveQuestion()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await MakeCodingQuestionCurrentAsync(sessionId);
        var (questionId, _) = await PrepareCodingQuestionEvaluatedAsync(sessionId);

        using var response = await userContext.Client.PostAsync("/api/v1/interview-session/question/submit-code", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var session = await GetSessionAsync(sessionId);
        var codingQuestion = session!.Questions.Single(x => x.Id == questionId);

        codingQuestion.Status.Should().Be(InterviewQuestionStatus.Submitted);
        session.Status.Should().Be(InterviewStatus.Finished);
    }

    [Fact]
    public async Task SkipQuestion_ShouldMarkCurrentQuestionSkipped_AndAdvanceToNextQuestion()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);

        using var response = await userContext.Client.PostAsync("/api/v1/interview-session/question/skip", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var session = await GetSessionAsync(sessionId);
        var firstQuestion = session!.Questions.OrderBy(x => x.OrderIndex).First();

        firstQuestion.Status.Should().Be(InterviewQuestionStatus.Skipped);

        using var currentQuestionResponse = await userContext.Client.GetAsync("/api/v1/interview-session/question");
        var currentQuestion = await currentQuestionResponse.Content.ReadFromJsonAsync<CurrentInterviewQuestionResponse>(ApiJsonOptions);

        currentQuestion.Should().NotBeNull();
        currentQuestion.OrderIndex.Should().Be(2);
    }

    [Fact]
    public async Task FinishSession_ShouldFinishSession_AndMarkStartedQuestionSkipped()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);

        using (var startResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await userContext.Client.PostAsync("/api/v1/interview-session/finish", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var session = await GetSessionAsync(sessionId);
        var orderedQuestions = session!.Questions.OrderBy(x => x.OrderIndex).ToList();

        session.Status.Should().Be(InterviewStatus.Finished);
        orderedQuestions[0].Status.Should().Be(InterviewQuestionStatus.Skipped);
        orderedQuestions[1].Status.Should().Be(InterviewQuestionStatus.NotStarted);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnFinishedSessionsOrderedByMostRecentFirst()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var olderSessionId = await CreateSessionAsync(userContext);
        using (var olderStartResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            olderStartResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        using (var olderFinishResponse = await userContext.Client.PostAsync("/api/v1/interview-session/finish", content: null))
        {
            olderFinishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        await SetSessionFinishedAtAsync(olderSessionId, new DateTime(2026, 04, 15, 8, 0, 0, DateTimeKind.Utc));

        var newerSessionId = await CreateSessionAsync(userContext);
        using (var newerStartResponse = await userContext.Client.PostAsync("/api/v1/interview-session/question/start", content: null))
        {
            newerStartResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        using (var newerFinishResponse = await userContext.Client.PostAsync("/api/v1/interview-session/finish", content: null))
        {
            newerFinishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        await SetSessionFinishedAtAsync(newerSessionId, new DateTime(2026, 04, 15, 9, 0, 0, DateTimeKind.Utc));

        using var response = await userContext.Client.GetAsync("/api/v1/interview-sessions/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<InterviewSessionHistoryItemResponse>>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload.Select(x => x.SessionId).Take(2).Should().Equal(newerSessionId, olderSessionId);
        payload.Should().OnlyContain(x => x.Status == InterviewStatus.Finished);
    }

    private sealed record CreateSessionRequest(Guid InterviewPresetId);

    private sealed record SubmitTheoryRequest(string Answer);

    private sealed record SubmitDraftCodeRequest(string Code);

    private sealed record ErrorResponse(string Code, string Description);

    private sealed record ValidationErrorResponse(
        string Code,
        string Description,
        Dictionary<string, string[]> Errors);

    private sealed record CurrentInterviewSessionResponse(
        Guid SessionId,
        InterviewStatus Status,
        DateTime StartedAt,
        DateTime PlannedEndAt,
        int TotalQuestions,
        int AnsweredQuestions);

    private sealed record CurrentInterviewQuestionResponse(
        Guid QuestionId,
        int OrderIndex,
        InterviewQuestionType Type,
        InterviewQuestionStatus Status,
        string? Answer,
        string? ProgrammingLanguageCode,
        int? TimeLimitMs,
        int? MemoryLimitMb,
        InterviewVerdict OverallVerdict,
        string? ErrorMessage,
        IReadOnlyList<InterviewTestCaseResponse> TestCases);

    private sealed record InterviewTestCaseResponse(
        int OrderIndex,
        string Input,
        string ExpectedOutput,
        string? ActualOutput,
        InterviewVerdict Verdict,
        double? ExecutionTimeMs,
        double? MemoryUsedMb,
        string? ErrorMessage);

    private sealed record InterviewSessionHistoryItemResponse(
        Guid SessionId,
        Guid InterviewPresetId,
        string InterviewPresetName,
        InterviewStatus Status,
        string SessionVerdict,
        DateTime StartedAt,
        DateTime PlannedEndAt,
        DateTime? FinishedAt,
        int TotalQuestions,
        int CompletedQuestions);
}
