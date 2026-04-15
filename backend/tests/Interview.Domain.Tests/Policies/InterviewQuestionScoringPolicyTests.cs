using System.Reflection;
using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Domain.Policies;

namespace Interview.Domain.Tests.Policies;

public class InterviewQuestionScoringPolicyTests
{
    [Fact]
    public void Resolve_ShouldReturnZero_WhenQuestionIsNotStarted()
    {
        var question = CreateTheoryQuestion();

        var score = InterviewQuestionScoringPolicy.Resolve(question);

        score.Should().Be(0);
    }

    [Fact]
    public void Resolve_ShouldReturnZero_WhenQuestionIsSkipped()
    {
        var question = CreateTheoryQuestion();
        question.Skip();

        var score = InterviewQuestionScoringPolicy.Resolve(question);

        score.Should().Be(0);
    }

    [Theory]
    [InlineData(QuestionVerdict.Correct, 8)]
    [InlineData(QuestionVerdict.PartiallyCorrect, 5)]
    [InlineData(QuestionVerdict.Incorrect, 2)]
    [InlineData(QuestionVerdict.None, 0)]
    public void Resolve_ShouldMapQuestionVerdictToScore_WhenQuestionWasAnswered(
        QuestionVerdict verdict,
        int expectedScore)
    {
        var question = CreateTheoryQuestion();
        question.Start(DateTime.UtcNow.AddMinutes(-2));
        question.SubmitTheoryAnswer("answer", DateTime.UtcNow.AddMinutes(-1));
        SetAutoProperty(question, nameof(InterviewQuestion.QuestionVerdict), verdict);

        var score = InterviewQuestionScoringPolicy.Resolve(question);

        score.Should().Be(expectedScore);
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

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        field.Should().NotBeNull($"auto-property field for {propertyName} should exist");
        field!.SetValue(target, value);
    }
}
