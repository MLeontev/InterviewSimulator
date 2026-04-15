using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class SubmitTheoryAnswerTests
{
    [Fact]
    public void SubmitTheoryAnswer_ShouldReturnFailure_WhenQuestionIsNotStarted()
    {
        var question = CreateTheoryQuestion();
        var now = DateTime.UtcNow;

        var result = question.SubmitTheoryAnswer("  my answer  ", now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_IN_PROGRESS");
        question.Status.Should().Be(QuestionStatus.NotStarted);
        question.Answer.Should().BeNull();
    }

    [Fact]
    public void SubmitTheoryAnswer_ShouldSetSubmittedState_WhenQuestionIsInProgress()
    {
        var question = CreateTheoryQuestion();
        var startedAt = DateTime.UtcNow.AddMinutes(-1);
        var submittedAt = DateTime.UtcNow;

        question.Start(startedAt);

        var result = question.SubmitTheoryAnswer("answer", submittedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Submitted);
        question.Answer.Should().Be("answer");
        question.StartedAt.Should().Be(startedAt);
        question.SubmittedAt.Should().Be(submittedAt);
        question.EvaluatedAt.Should().BeNull();
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
        question.ErrorMessage.Should().BeNull();
        question.AiFeedbackJson.Should().BeNull();
    }

    [Fact]
    public void SubmitTheoryAnswer_ShouldReturnFailure_WhenQuestionTypeIsNotTheory()
    {
        var question = InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: "Coding",
            text: "Solve task",
            type: QuestionType.Coding,
            orderIndex: 1,
            referenceSolution: "solution",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: "csharp",
            timeLimitMs: 1000,
            memoryLimitMb: 64);

        var result = question.SubmitTheoryAnswer("answer", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_THEORY");
        question.Status.Should().Be(QuestionStatus.NotStarted);
        question.Answer.Should().BeNull();
    }

    [Fact]
    public void SubmitTheoryAnswer_ShouldReturnFailure_WhenQuestionIsSkipped()
    {
        var question = CreateTheoryQuestion();
        question.Skip();

        var result = question.SubmitTheoryAnswer("answer", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_IN_PROGRESS");
        question.Status.Should().Be(QuestionStatus.Skipped);
        question.Answer.Should().BeNull();
    }

    [Fact]
    public void SubmitTheoryAnswer_ShouldReturnFailure_WhenQuestionIsAlreadySubmitted()
    {
        var question = CreateTheoryQuestion();
        question.Start(DateTime.UtcNow.AddMinutes(-2));
        question.SubmitTheoryAnswer("first answer", DateTime.UtcNow.AddMinutes(-1));

        var result = question.SubmitTheoryAnswer("second answer", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_IN_PROGRESS");
        question.Status.Should().Be(QuestionStatus.Submitted);
        question.Answer.Should().Be("first answer");
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
}
