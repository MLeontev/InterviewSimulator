using System.Reflection;
using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.InterviewSessionTests;

public class AiEvaluationTests
{
    [Fact]
    public void HasPendingQuestionsForAiEvaluation_ShouldReturnTrue_WhenSessionHasSubmittedQuestion()
    {
        var question = CreateTheoryQuestion(orderIndex: 1);
        question.Start(DateTime.UtcNow.AddMinutes(-1));
        question.SubmitTheoryAnswer("answer", DateTime.UtcNow);
        var session = CreateSession([question]);

        var result = session.HasPendingQuestionsForAiEvaluation();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasPendingQuestionsForAiEvaluation_ShouldReturnFalse_WhenQuestionsAreNotPending()
    {
        var question = CreateTheoryQuestion(orderIndex: 1);
        question.Skip();
        var session = CreateSession([question]);

        var result = session.HasPendingQuestionsForAiEvaluation();

        result.Should().BeFalse();
    }

    [Fact]
    public void CancelAiEvaluation_ShouldSetFinished_WhenSessionIsEvaluatingAi()
    {
        var session = CreateSession();
        SetAutoProperty(session, nameof(InterviewSession.Status), InterviewStatus.EvaluatingAi);

        var result = session.CancelAiEvaluation();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(InterviewStatus.Finished);
    }

    [Fact]
    public void ApplyAiEvaluationSuccess_ShouldSetEvaluatedVerdictAndResetRetryState_WhenSessionIsEvaluatingAi()
    {
        var session = CreateSession();
        var nextRetryAt = DateTime.UtcNow.AddMinutes(10);

        SetAutoProperty(session, nameof(InterviewSession.Status), InterviewStatus.EvaluatingAi);
        SetAutoProperty(session, nameof(InterviewSession.AiRetryCount), 2);
        SetAutoProperty<DateTime?>(session, nameof(InterviewSession.AiNextRetryAt), nextRetryAt);

        var result = session.ApplyAiEvaluationSuccess("{\"summary\":\"ok\"}", overallScore: 8.2);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(InterviewStatus.Evaluated);
        session.SessionVerdict.Should().Be(SessionVerdict.Passed);
        session.AiFeedbackJson.Should().Be("{\"summary\":\"ok\"}");
        session.AiRetryCount.Should().Be(0);
        session.AiNextRetryAt.Should().BeNull();
        session.FinishedAt.Should().BeNull();
    }

    [Fact]
    public void ApplyAiEvaluationSuccess_ShouldReturnFailure_WhenSessionIsNotEvaluatingAi()
    {
        var session = CreateSession();

        var result = session.ApplyAiEvaluationSuccess("{\"summary\":\"ok\"}", overallScore: 8.2);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SESSION_NOT_IN_EVALUATING_AI");
    }

    [Fact]
    public void ScheduleAiEvaluationRetry_ShouldSetFinishedAndRetryState_WhenSessionIsEvaluatingAi()
    {
        var session = CreateSession();
        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        SetAutoProperty(session, nameof(InterviewSession.Status), InterviewStatus.EvaluatingAi);

        var result = session.ScheduleAiEvaluationRetry(nextRetryCount: 3, nextRetryAtUtc: nextRetryAt);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(InterviewStatus.Finished);
        session.AiRetryCount.Should().Be(3);
        session.AiNextRetryAt.Should().Be(nextRetryAt);
        session.FinishedAt.Should().BeNull();
    }

    [Fact]
    public void MarkAiEvaluationFailed_ShouldSetFailedStatusVerdictAndFallbackFeedback_WhenSessionIsEvaluatingAi()
    {
        var session = CreateSession();

        SetAutoProperty(session, nameof(InterviewSession.Status), InterviewStatus.EvaluatingAi);

        var result = session.MarkAiEvaluationFailed(
            retryCount: 4,
            overallScore: 5.5,
            fallbackAiFeedbackJson: "{\"summary\":\"fallback\"}");

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(InterviewStatus.AiEvaluationFailed);
        session.SessionVerdict.Should().Be(SessionVerdict.Borderline);
        session.AiRetryCount.Should().Be(4);
        session.AiNextRetryAt.Should().BeNull();
        session.AiFeedbackJson.Should().Be("{\"summary\":\"fallback\"}");
    }

    [Fact]
    public void ResetForAiRetry_ShouldResetFailedQuestionsAndSessionState_WhenThereAreRetriableQuestions()
    {
        var question = CreateTheoryQuestion(orderIndex: 1);
        question.Start(DateTime.UtcNow.AddMinutes(-11));
        question.SubmitTheoryAnswer("candidate answer", DateTime.UtcNow.AddMinutes(-10));

        SetAutoProperty(question, nameof(InterviewQuestion.Status), QuestionStatus.AiEvaluationFailed);
        SetAutoProperty(question, nameof(InterviewQuestion.AiRetryCount), 2);
        SetAutoProperty<DateTime?>(question, nameof(InterviewQuestion.AiNextRetryAt), DateTime.UtcNow.AddMinutes(3));
        SetAutoProperty(question, nameof(InterviewQuestion.AiFeedbackJson), "{\"summary\":\"old\"}");
        SetAutoProperty(question, nameof(InterviewQuestion.ErrorMessage), "AI error");
        SetAutoProperty(question, nameof(InterviewQuestion.QuestionVerdict), QuestionVerdict.Correct);
        SetAutoProperty<DateTime?>(question, nameof(InterviewQuestion.EvaluatedAt), DateTime.UtcNow.AddMinutes(-2));

        var session = CreateSession([question]);
        var now = DateTime.UtcNow;

        SetAutoProperty(session, nameof(InterviewSession.Status), InterviewStatus.AiEvaluationFailed);
        SetAutoProperty(session, nameof(InterviewSession.AiRetryCount), 5);
        SetAutoProperty<DateTime?>(session, nameof(InterviewSession.AiNextRetryAt), DateTime.UtcNow.AddMinutes(7));
        SetAutoProperty(session, nameof(InterviewSession.AiFeedbackJson), "{\"summary\":\"session\"}");

        var result = session.ResetForAiRetry(now);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(InterviewStatus.Finished);
        session.AiRetryCount.Should().Be(0);
        session.AiNextRetryAt.Should().BeNull();
        session.AiFeedbackJson.Should().BeNull();

        question.Status.Should().Be(QuestionStatus.Submitted);
        question.SubmittedAt.Should().Be(now);
        question.EvaluatedAt.Should().BeNull();
        question.AiRetryCount.Should().Be(0);
        question.AiNextRetryAt.Should().BeNull();
        question.AiFeedbackJson.Should().BeNull();
        question.ErrorMessage.Should().BeNull();
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
    }

    [Fact]
    public void ResetForAiRetry_ShouldReturnFailure_WhenThereAreNoRetriableQuestions()
    {
        var question = CreateTheoryQuestion(orderIndex: 1);
        var session = CreateSession([question]);

        SetAutoProperty(session, nameof(InterviewSession.Status), InterviewStatus.AiEvaluationFailed);

        var result = session.ResetForAiRetry(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NO_FAILED_AI_QUESTIONS");
    }

    private static InterviewSession CreateSession(IReadOnlyCollection<InterviewQuestion>? questions = null)
    {
        return InterviewSession.Create(
            id: Guid.NewGuid(),
            candidateId: Guid.NewGuid(),
            interviewPresetId: Guid.NewGuid(),
            interviewPresetName: "Preset",
            startedAt: DateTime.UtcNow.AddHours(-1),
            plannedEndAt: DateTime.UtcNow.AddHours(1),
            questions: questions);
    }

    private static InterviewQuestion CreateTheoryQuestion(int orderIndex)
    {
        return InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: $"Theory {orderIndex}",
            text: "Explain something",
            type: QuestionType.Theory,
            orderIndex: orderIndex,
            referenceSolution: "reference",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: null,
            timeLimitMs: null,
            memoryLimitMb: null);
    }

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        field.Should().NotBeNull($"auto-property field for {propertyName} should exist");
        field.SetValue(target, value);
    }
}
