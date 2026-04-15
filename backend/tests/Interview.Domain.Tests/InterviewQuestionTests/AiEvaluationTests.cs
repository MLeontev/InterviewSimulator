using System.Reflection;
using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class AiEvaluationTests
{
    [Theory]
    [InlineData(2, QuestionVerdict.Incorrect)]
    [InlineData(5, QuestionVerdict.PartiallyCorrect)]
    [InlineData(8, QuestionVerdict.Correct)]
    public void ApplyAiEvaluationSuccess_ShouldSetEvaluatedAiAndMapVerdict_WhenQuestionIsEvaluatingAi(
        int score,
        QuestionVerdict expectedVerdict)
    {
        var question = CreateEvaluatingAiTheoryQuestion();
        var evaluatedAt = DateTime.UtcNow;

        SetAutoProperty(question, nameof(InterviewQuestion.AiRetryCount), 2);
        SetAutoProperty<DateTime?>(question, nameof(InterviewQuestion.AiNextRetryAt), DateTime.UtcNow.AddMinutes(5));
        SetAutoProperty(question, nameof(InterviewQuestion.ErrorMessage), "AI error");

        var result = question.ApplyAiEvaluationSuccess("{\"summary\":\"ok\"}", score, evaluatedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.EvaluatedAi);
        question.EvaluatedAt.Should().Be(evaluatedAt);
        question.QuestionVerdict.Should().Be(expectedVerdict);
        question.AiFeedbackJson.Should().Be("{\"summary\":\"ok\"}");
        question.ErrorMessage.Should().BeNull();
        question.AiRetryCount.Should().Be(0);
        question.AiNextRetryAt.Should().BeNull();
    }

    [Fact]
    public void ApplyAiEvaluationSuccess_ShouldReturnFailure_WhenQuestionIsNotEvaluatingAi()
    {
        var question = CreateSubmittedTheoryQuestion();

        var result = question.ApplyAiEvaluationSuccess("{\"summary\":\"ok\"}", 8, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_IN_EVALUATING_AI");
        question.Status.Should().Be(QuestionStatus.Submitted);
    }

    [Fact]
    public void ScheduleAiEvaluationRetry_ShouldSetSubmittedAndRetryState_WhenQuestionIsEvaluatingAi()
    {
        var question = CreateEvaluatingAiTheoryQuestion();
        var nextRetryAt = DateTime.UtcNow.AddMinutes(15);

        var result = question.ScheduleAiEvaluationRetry(
            nextRetryCount: 2,
            nextRetryAtUtc: nextRetryAt,
            maxRetries: 5);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Submitted);
        question.AiRetryCount.Should().Be(2);
        question.AiNextRetryAt.Should().Be(nextRetryAt);
        question.ErrorMessage.Should().Be("AI временно недоступен. Повтор 2/5.");
        question.EvaluatedAt.Should().BeNull();
    }

    [Fact]
    public void MarkAiEvaluationFailed_ShouldSetFailedStatusAndError_WhenQuestionIsEvaluatingAi()
    {
        var question = CreateEvaluatingAiTheoryQuestion();
        var failedAt = DateTime.UtcNow;

        SetAutoProperty<DateTime?>(question, nameof(InterviewQuestion.AiNextRetryAt), DateTime.UtcNow.AddMinutes(5));

        var result = question.MarkAiEvaluationFailed(retryCount: 4, nowUtc: failedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.AiEvaluationFailed);
        question.EvaluatedAt.Should().Be(failedAt);
        question.AiRetryCount.Should().Be(4);
        question.AiNextRetryAt.Should().BeNull();
        question.ErrorMessage.Should().Be("AI-оценка недоступна после нескольких попыток.");
    }

    [Fact]
    public void ResetForAiRetry_ShouldResetState_WhenQuestionHasFailedAiEvaluationAndAnswer()
    {
        var question = CreateSubmittedTheoryQuestion();
        var now = DateTime.UtcNow;

        SetAutoProperty(question, nameof(InterviewQuestion.Status), QuestionStatus.AiEvaluationFailed);
        SetAutoProperty(question, nameof(InterviewQuestion.AiRetryCount), 3);
        SetAutoProperty<DateTime?>(question, nameof(InterviewQuestion.AiNextRetryAt), DateTime.UtcNow.AddMinutes(7));
        SetAutoProperty<DateTime?>(question, nameof(InterviewQuestion.EvaluatedAt), DateTime.UtcNow.AddMinutes(-1));
        SetAutoProperty(question, nameof(InterviewQuestion.AiFeedbackJson), "{\"summary\":\"old\"}");
        SetAutoProperty(question, nameof(InterviewQuestion.ErrorMessage), "AI error");
        SetAutoProperty(question, nameof(InterviewQuestion.QuestionVerdict), QuestionVerdict.Correct);

        var result = question.ResetForAiRetry(now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Submitted);
        question.Answer.Should().Be("candidate answer");
        question.SubmittedAt.Should().Be(now);
        question.EvaluatedAt.Should().BeNull();
        question.AiRetryCount.Should().Be(0);
        question.AiNextRetryAt.Should().BeNull();
        question.AiFeedbackJson.Should().BeNull();
        question.ErrorMessage.Should().BeNull();
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
    }

    [Fact]
    public void ResetForAiRetry_ShouldReturnFalse_WhenQuestionIsNotAiEvaluationFailed()
    {
        var question = CreateSubmittedTheoryQuestion();
        var submittedAt = question.SubmittedAt;

        var result = question.ResetForAiRetry(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        question.Status.Should().Be(QuestionStatus.Submitted);
        question.SubmittedAt.Should().Be(submittedAt);
        question.Answer.Should().Be("candidate answer");
    }

    [Fact]
    public void ResetForAiRetry_ShouldReturnFalse_WhenQuestionHasNoAnswer()
    {
        var question = CreateTheoryQuestion();

        SetAutoProperty(question, nameof(InterviewQuestion.Status), QuestionStatus.AiEvaluationFailed);

        var result = question.ResetForAiRetry(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        question.Status.Should().Be(QuestionStatus.AiEvaluationFailed);
        question.Answer.Should().BeNull();
    }

    private static InterviewQuestion CreateTheoryQuestion()
    {
        return InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: "Theory",
            text: "Explain something",
            type: QuestionType.Theory,
            orderIndex: 1,
            referenceSolution: "reference",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: null,
            timeLimitMs: null,
            memoryLimitMb: null);
    }

    private static InterviewQuestion CreateSubmittedTheoryQuestion()
    {
        var question = CreateTheoryQuestion();
        question.Start(DateTime.UtcNow.AddMinutes(-2));
        question.SubmitTheoryAnswer("candidate answer", DateTime.UtcNow.AddMinutes(-1));

        return question;
    }

    private static InterviewQuestion CreateEvaluatingAiTheoryQuestion()
    {
        var question = CreateSubmittedTheoryQuestion();
        SetAutoProperty(question, nameof(InterviewQuestion.Status), QuestionStatus.EvaluatingAi);

        return question;
    }

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        field.Should().NotBeNull($"auto-property field for {propertyName} should exist");
        field!.SetValue(target, value);
    }
}
