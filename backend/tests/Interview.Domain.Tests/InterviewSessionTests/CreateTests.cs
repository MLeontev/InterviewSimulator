using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.InterviewSessionTests;

public class CreateTests
{
    [Fact]
    public void Create_ShouldInitializeSessionWithProvidedValues()
    {
        var id = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddMinutes(-30);
        var plannedEndAt = DateTime.UtcNow.AddMinutes(30);

        var session = InterviewSession.Create(
            id: id,
            candidateId: candidateId,
            interviewPresetId: presetId,
            interviewPresetName: "Backend preset",
            startedAt: startedAt,
            plannedEndAt: plannedEndAt);

        session.Id.Should().Be(id);
        session.CandidateId.Should().Be(candidateId);
        session.InterviewPresetId.Should().Be(presetId);
        session.InterviewPresetName.Should().Be("Backend preset");
        session.StartedAt.Should().Be(startedAt);
        session.PlannedEndAt.Should().Be(plannedEndAt);
    }

    [Fact]
    public void Create_ShouldSetDefaultState()
    {
        var session = InterviewSession.Create(
            id: Guid.NewGuid(),
            candidateId: Guid.NewGuid(),
            interviewPresetId: Guid.NewGuid(),
            interviewPresetName: "Preset",
            startedAt: DateTime.UtcNow.AddHours(-1),
            plannedEndAt: DateTime.UtcNow.AddHours(1));

        session.Status.Should().Be(InterviewStatus.InProgress);
        session.SessionVerdict.Should().Be(SessionVerdict.None);
        session.FinishedAt.Should().BeNull();
        session.AiFeedbackJson.Should().BeNull();
        session.AiRetryCount.Should().Be(0);
        session.AiNextRetryAt.Should().BeNull();
        session.Questions.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldOrderQuestionsByOrderIndex()
    {
        var question2 = CreateTheoryQuestion(orderIndex: 2);
        var question1 = CreateTheoryQuestion(orderIndex: 1);
        var question3 = CreateTheoryQuestion(orderIndex: 3);

        var session = InterviewSession.Create(
            id: Guid.NewGuid(),
            candidateId: Guid.NewGuid(),
            interviewPresetId: Guid.NewGuid(),
            interviewPresetName: "Preset",
            startedAt: DateTime.UtcNow.AddHours(-1),
            plannedEndAt: DateTime.UtcNow.AddHours(1),
            questions: [question2, question1, question3]);

        session.Questions.Should().HaveCount(3);
        session.Questions.Select(x => x.OrderIndex).Should().ContainInOrder(1, 2, 3);
        session.Questions[0].Should().BeSameAs(question1);
        session.Questions[1].Should().BeSameAs(question2);
        session.Questions[2].Should().BeSameAs(question3);
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
}
