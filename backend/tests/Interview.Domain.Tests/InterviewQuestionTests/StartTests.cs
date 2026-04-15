using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class StartTests
{
    [Fact]
    public void Start_ShouldSetInProgressAndStartedAt_WhenQuestionIsNotStarted()
    {
        var question = CreateTheoryQuestion();
        var startedAt = DateTime.UtcNow;

        var result = question.Start(startedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.InProgress);
        question.StartedAt.Should().Be(startedAt);
    }

    [Fact]
    public void Start_ShouldReturnSuccessWithoutChangingStartedAt_WhenQuestionAlreadyInProgress()
    {
        var question = CreateTheoryQuestion();
        var firstStartedAt = DateTime.UtcNow.AddMinutes(-2);
        var secondStartedAt = DateTime.UtcNow;

        question.Start(firstStartedAt);

        var result = question.Start(secondStartedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.InProgress);
        question.StartedAt.Should().Be(firstStartedAt);
    }

    [Fact]
    public void Start_ShouldReturnFailure_WhenQuestionIsAlreadySubmitted()
    {
        var question = CreateTheoryQuestion();
        var startedAt = DateTime.UtcNow.AddMinutes(-2);

        question.Start(startedAt);
        question.SubmitTheoryAnswer("answer", DateTime.UtcNow.AddMinutes(-1));

        var result = question.Start(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_CANNOT_BE_STARTED");
        question.Status.Should().Be(QuestionStatus.Submitted);
        question.StartedAt.Should().Be(startedAt);
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
