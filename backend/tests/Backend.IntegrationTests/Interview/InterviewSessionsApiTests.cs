using System.Net;
using System.Net.Http.Json;
using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using Interview.UseCases.Commands;
using InterviewQuestionStatus = Interview.Domain.Enums.QuestionStatus;
using InterviewQuestionType = Interview.Domain.Enums.QuestionType;
using InterviewQuestionVerdict = Interview.Domain.Enums.QuestionVerdict;
using InterviewSessionVerdict = Interview.Domain.Enums.SessionVerdict;
using InterviewStatus = Interview.Domain.Enums.InterviewStatus;
using InterviewVerdict = Interview.Domain.Enums.Verdict;

namespace Backend.IntegrationTests.Interview;

public sealed class InterviewSessionsApiTests : InterviewIntegrationTestBase
{
    public InterviewSessionsApiTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetReport_ShouldReturnReport_WhenSessionEvaluated()
    {
        ResetFakeAiEvaluationService();

        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await LeaveOnlyFirstQuestionActiveAsync(sessionId);

        var startResult = await SendAsync(new StartCurrentInterviewQuestionCommand(userContext.UserId));
        startResult.IsSuccess.Should().BeTrue();

        var submitResult = await SendAsync(new SubmitTheoryAnswerCommand(userContext.UserId, "  theory answer  "));
        submitResult.IsSuccess.Should().BeTrue();

        var sessionAfterSubmit = await GetSessionAsync(sessionId);
        var evaluatedQuestionId = sessionAfterSubmit!.Questions.OrderBy(x => x.OrderIndex).First().Id;
        var totalQuestions = sessionAfterSubmit.Questions.Count;

        await EvaluateTheoryQuestionAsync(evaluatedQuestionId);
        await EvaluateSessionAsync(sessionId);

        using var response = await userContext.Client.GetAsync($"/api/v1/interview-sessions/{sessionId}/report");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<InterviewSessionReportResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload.SessionId.Should().Be(sessionId);
        payload.CandidateId.Should().Be(userContext.UserId);
        payload.Status.Should().Be(InterviewStatus.Evaluated);
        payload.SessionVerdict.Should().NotBe(InterviewSessionVerdict.None);
        payload.TotalQuestions.Should().Be(totalQuestions);
        payload.AnsweredQuestions.Should().Be(1);
        payload.AverageQuestionAiScore.Should().BeApproximately(Math.Round(8d / totalQuestions, 2), 0.001);
        payload.SessionSummary.Should().Be("Mocked session summary");
        payload.SessionStrengths.Should().Contain("Good understanding");
        payload.SessionWeaknesses.Should().Contain("Needs improvement");
        payload.SessionRecommendations.Should().Contain("Practice more");
        payload.Questions.Should().HaveCount(totalQuestions);
        payload.Questions.Select(x => x.OrderIndex).Should().Equal(sessionAfterSubmit.Questions.Select(x => x.OrderIndex));

        var evaluatedQuestion = payload.Questions.Single(x => x.Status == InterviewQuestionStatus.EvaluatedAi);

        evaluatedQuestion.Type.Should().Be(InterviewQuestionType.Theory);
        evaluatedQuestion.Answer.Should().Be("theory answer");
        evaluatedQuestion.AiScore.Should().Be(8);
        evaluatedQuestion.AiFeedback.Should().Be("Mocked theory feedback");
        evaluatedQuestion.QuestionVerdict.Should().Be(InterviewQuestionVerdict.Correct);
        evaluatedQuestion.OverallVerdict.Should().Be(InterviewVerdict.None);
        evaluatedQuestion.PassedTests.Should().BeNull();
        evaluatedQuestion.TotalTests.Should().BeNull();

        payload.Questions.Count(x => x.Status == InterviewQuestionStatus.Skipped).Should().Be(totalQuestions - 1);
    }

    [Fact]
    public async Task GetReport_ShouldReturnUnprocessableEntity_WhenSessionIsNotEvaluated()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);

        using var response = await userContext.Client.GetAsync($"/api/v1/interview-sessions/{sessionId}/report");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload.Code.Should().Be("SESSION_NOT_FINISHED");
        payload.Description.Should().Be("Сессия еще не оценена");
    }

    [Fact]
    public async Task RetryAiEvaluation_ShouldResetSessionAndFailedQuestions_WhenSessionEvaluationFailed()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await LeaveOnlyFirstQuestionActiveAsync(sessionId);

        var startResult = await SendAsync(new StartCurrentInterviewQuestionCommand(userContext.UserId));
        startResult.IsSuccess.Should().BeTrue();

        var submitResult = await SendAsync(new SubmitTheoryAnswerCommand(userContext.UserId, "answer for retry"));
        submitResult.IsSuccess.Should().BeTrue();

        var sessionBeforeRetry = await GetSessionAsync(sessionId);
        var questionId = sessionBeforeRetry!.Questions.OrderBy(x => x.OrderIndex).First().Id;

        await MarkQuestionAiEvaluationFailedAsync(questionId, retryCount: 3);
        await MarkSessionAiEvaluationFailedAsync(sessionId, retryCount: 2);

        using var response = await userContext.Client.PostAsync(
            $"/api/v1/interview-sessions/{sessionId}/ai-retry",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var savedSession = await GetSessionAsync(sessionId);
        var savedQuestion = savedSession!.Questions.Single(x => x.Id == questionId);

        savedSession.Status.Should().Be(InterviewStatus.Finished);
        savedSession.AiRetryCount.Should().Be(0);
        savedSession.AiFeedbackJson.Should().BeNull();

        savedQuestion.Status.Should().Be(InterviewQuestionStatus.Submitted);
        savedQuestion.Answer.Should().Be("answer for retry");
        savedQuestion.QuestionVerdict.Should().Be(InterviewQuestionVerdict.None);
        savedQuestion.AiRetryCount.Should().Be(0);
        savedQuestion.AiFeedbackJson.Should().BeNull();
        savedQuestion.ErrorMessage.Should().BeNull();
        savedQuestion.EvaluatedAt.Should().BeNull();
    }

    [Fact]
    public async Task RetryAiEvaluation_ShouldReturnUnprocessableEntity_WhenSessionDidNotFailAiEvaluation()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);

        using var response = await userContext.Client.PostAsync(
            $"/api/v1/interview-sessions/{sessionId}/ai-retry",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiJsonOptions);

        payload.Should().NotBeNull();
        payload.Code.Should().Be("SESSION_NOT_FAILED");
        payload.Description.Should().Be("ИИ-оценка сессии не была завершена с ошибкой");
    }

    private sealed class InterviewSessionReportResponse
    {
        public Guid SessionId { get; init; }
        public Guid CandidateId { get; init; }
        public InterviewStatus Status { get; init; }
        public InterviewSessionVerdict SessionVerdict { get; init; }
        public int TotalQuestions { get; init; }
        public int AnsweredQuestions { get; init; }
        public double AverageQuestionAiScore { get; init; }
        public string? SessionSummary { get; init; }
        public IReadOnlyList<string> SessionStrengths { get; init; } = [];
        public IReadOnlyList<string> SessionWeaknesses { get; init; } = [];
        public IReadOnlyList<string> SessionRecommendations { get; init; } = [];
        public IReadOnlyList<InterviewSessionReportQuestionResponse> Questions { get; init; } = [];
    }

    private sealed class InterviewSessionReportQuestionResponse
    {
        public Guid QuestionId { get; init; }
        public int OrderIndex { get; init; }
        public InterviewQuestionType Type { get; init; }
        public InterviewQuestionStatus Status { get; init; }
        public InterviewQuestionVerdict QuestionVerdict { get; init; }
        public InterviewVerdict OverallVerdict { get; init; }
        public string? Answer { get; init; }
        public int? AiScore { get; init; }
        public string? AiFeedback { get; init; }
        public int? PassedTests { get; init; }
        public int? TotalTests { get; init; }
    }

    private sealed record ErrorResponse(string Code, string Description);
}
