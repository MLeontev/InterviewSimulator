using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using Interview.Infrastructure.Workers;
using Interview.UseCases.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using InterviewQuestionStatus = Interview.Domain.Enums.QuestionStatus;
using InterviewQuestionVerdict = Interview.Domain.Enums.QuestionVerdict;
using InterviewSessionVerdict = Interview.Domain.Enums.SessionVerdict;
using InterviewStatus = Interview.Domain.Enums.InterviewStatus;

namespace Backend.IntegrationTests.Interview;

public sealed class InterviewWorkerTests : InterviewIntegrationTestBase
{
    public InterviewWorkerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ProcessAnswerAsync_ShouldEvaluateTheoryQuestion()
    {
        await ClearInterviewStateAsync();
        ResetFakeAiEvaluationService();

        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await LeaveOnlyFirstQuestionActiveAsync(sessionId);

        var startResult = await SendAsync(new StartCurrentInterviewQuestionCommand(userContext.UserId));
        startResult.IsSuccess.Should().BeTrue();

        var submitResult = await SendAsync(new SubmitTheoryAnswerCommand(userContext.UserId, "theory answer"));
        submitResult.IsSuccess.Should().BeTrue();

        var session = await GetSessionAsync(sessionId);
        var theoryQuestionId = session!.Questions.OrderBy(x => x.OrderIndex).First().Id;

        var processed = await CreateAiAnswerWorker().ProcessAnswerAsync(CancellationToken.None);

        var savedQuestion = await GetQuestionAsync(theoryQuestionId);

        processed.Should().BeTrue();
        savedQuestion.Should().NotBeNull();
        savedQuestion.Status.Should().Be(InterviewQuestionStatus.EvaluatedAi);
        savedQuestion.QuestionVerdict.Should().Be(InterviewQuestionVerdict.Correct);
        savedQuestion.AiFeedbackJson.Should().Contain("\"score\":8");
        savedQuestion.AiFeedbackJson.Should().Contain("Mocked theory feedback");
        savedQuestion.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAnswerAsync_ShouldEvaluateCodingQuestion()
    {
        await ClearInterviewStateAsync();
        ResetFakeAiEvaluationService();

        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await MakeCodingQuestionCurrentAsync(sessionId);

        var (questionId, _) = await PrepareCodingQuestionEvaluatedAsync(sessionId);
        var submitResult = await SendAsync(new SubmitCodeAnswerCommand(userContext.UserId));

        submitResult.IsSuccess.Should().BeTrue();

        var processed = await CreateAiAnswerWorker().ProcessAnswerAsync(CancellationToken.None);

        var savedQuestion = await GetQuestionAsync(questionId);

        processed.Should().BeTrue();
        savedQuestion.Should().NotBeNull();
        savedQuestion.Status.Should().Be(InterviewQuestionStatus.EvaluatedAi);
        savedQuestion.QuestionVerdict.Should().Be(InterviewQuestionVerdict.Correct);
        savedQuestion.AiFeedbackJson.Should().Contain("\"score\":7");
        savedQuestion.AiFeedbackJson.Should().Contain("Mocked coding feedback");
        savedQuestion.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessSessionAsync_ShouldEvaluateFinishedSession_WhenQuestionsAreReady()
    {
        await ClearInterviewStateAsync();
        ResetFakeAiEvaluationService();

        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await LeaveOnlyFirstQuestionActiveAsync(sessionId);

        var startResult = await SendAsync(new StartCurrentInterviewQuestionCommand(userContext.UserId));
        startResult.IsSuccess.Should().BeTrue();

        var submitResult = await SendAsync(new SubmitTheoryAnswerCommand(userContext.UserId, "session answer"));
        submitResult.IsSuccess.Should().BeTrue();

        var sessionAfterSubmit = await GetSessionAsync(sessionId);
        var theoryQuestionId = sessionAfterSubmit!.Questions.OrderBy(x => x.OrderIndex).First().Id;

        var answerProcessed = await CreateAiAnswerWorker().ProcessAnswerAsync(CancellationToken.None);
        answerProcessed.Should().BeTrue();

        var processed = await CreateAiSessionWorker().ProcessSessionAsync(CancellationToken.None);

        var savedSession = await GetSessionAsync(sessionId);

        processed.Should().BeTrue();
        savedSession.Should().NotBeNull();
        savedSession.Status.Should().Be(InterviewStatus.Evaluated);
        savedSession.SessionVerdict.Should().NotBe(InterviewSessionVerdict.None);
        savedSession.AiFeedbackJson.Should().Contain("Mocked session summary");

        var savedQuestion = await GetQuestionAsync(theoryQuestionId);
        savedQuestion.Should().NotBeNull();
        savedQuestion.Status.Should().Be(InterviewQuestionStatus.EvaluatedAi);
    }

    [Fact]
    public async Task ProcessTimedOutSessionAsync_ShouldFinishExpiredSession_AndSkipCurrentQuestion()
    {
        await ClearInterviewStateAsync();

        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);

        var startResult = await SendAsync(new StartCurrentInterviewQuestionCommand(userContext.UserId));
        startResult.IsSuccess.Should().BeTrue();

        await SetSessionPlannedEndAtAsync(sessionId, DateTime.UtcNow.AddMinutes(-1));

        var processed = await CreateSessionTimeoutWorker().ProcessTimedOutSessionAsync(CancellationToken.None);

        var savedSession = await GetSessionAsync(sessionId);
        var firstQuestion = savedSession!.Questions.OrderBy(x => x.OrderIndex).First();

        processed.Should().BeTrue();
        savedSession.Status.Should().Be(InterviewStatus.Finished);
        savedSession.FinishedAt.Should().NotBeNull();
        firstQuestion.Status.Should().Be(InterviewQuestionStatus.Skipped);
    }

    private AiAnswerEvaluationWorker CreateAiAnswerWorker()
    {
        return new AiAnswerEvaluationWorker(
            Factory.Services,
            NullLogger<AiAnswerEvaluationWorker>.Instance);
    }

    private AiSessionEvaluationWorker CreateAiSessionWorker()
    {
        return new AiSessionEvaluationWorker(
            Factory.Services,
            NullLogger<AiSessionEvaluationWorker>.Instance);
    }

    private SessionTimeoutWorker CreateSessionTimeoutWorker()
    {
        return new SessionTimeoutWorker(
            Factory.Services,
            NullLogger<SessionTimeoutWorker>.Instance);
    }
}
