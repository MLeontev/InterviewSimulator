using CodeExecution.Domain.Entities;
using CodeExecution.Domain.Enums;
using FluentAssertions;

namespace CodeExecution.Domain.Tests.CodeSubmissionTests;

public class LifecycleTests
{
    [Fact]
    public void MarkStarted_ShouldSetStartedAt_WhenCalledFirstTime()
    {
        var submission = CreateSubmission();
        var startedAt = DateTime.UtcNow;

        submission.MarkStarted(startedAt);

        submission.StartedAt.Should().Be(startedAt);
        submission.Status.Should().Be(ExecutionStatus.Pending);
    }

    [Fact]
    public void MarkStarted_ShouldNotOverrideStartedAt_WhenCalledAgain()
    {
        var submission = CreateSubmission();
        var firstStartedAt = DateTime.UtcNow.AddMinutes(-1);
        var secondStartedAt = DateTime.UtcNow;

        submission.MarkStarted(firstStartedAt);
        submission.MarkStarted(secondStartedAt);

        submission.StartedAt.Should().Be(firstStartedAt);
    }

    [Fact]
    public void Complete_ShouldSetCompletedState_WhenVerdictIsNotFailedSystem()
    {
        var submission = CreateSubmission();
        var completedAt = DateTime.UtcNow;

        submission.Complete(Verdict.OK, errorMessage: null, nowUtc: completedAt);

        submission.Status.Should().Be(ExecutionStatus.Completed);
        submission.OverallVerdict.Should().Be(Verdict.OK);
        submission.CompletedAt.Should().Be(completedAt);
        submission.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Complete_ShouldSetFailedStateAndError_WhenVerdictIsFailedSystem()
    {
        var submission = CreateSubmission();
        var completedAt = DateTime.UtcNow;

        submission.Complete(
            Verdict.FailedSystem,
            errorMessage: "infrastructure error",
            nowUtc: completedAt);

        submission.Status.Should().Be(ExecutionStatus.Failed);
        submission.OverallVerdict.Should().Be(Verdict.FailedSystem);
        submission.CompletedAt.Should().Be(completedAt);
        submission.ErrorMessage.Should().Be("infrastructure error");
    }

    private static CodeSubmission CreateSubmission()
    {
        return CodeSubmission.Create(
            id: Guid.NewGuid(),
            interviewQuestionId: Guid.NewGuid(),
            code: "Console.WriteLine(3);",
            languageCode: "csharp",
            createdAtUtc: DateTime.UtcNow.AddMinutes(-2),
            timeLimitMs: 1000,
            memoryLimitMb: 64);
    }
}
