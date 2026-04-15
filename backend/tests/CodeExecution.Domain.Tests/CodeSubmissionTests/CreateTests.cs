using CodeExecution.Domain.Entities;
using CodeExecution.Domain.Enums;
using FluentAssertions;

namespace CodeExecution.Domain.Tests.CodeSubmissionTests;

public class CreateTests
{
    [Fact]
    public void Create_ShouldInitializeSubmissionWithProvidedValues()
    {
        var submissionId = Guid.NewGuid();
        var interviewQuestionId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var submission = CodeSubmission.Create(
            id: submissionId,
            interviewQuestionId: interviewQuestionId,
            code: "Console.WriteLine(3);",
            languageCode: "csharp",
            createdAtUtc: createdAt,
            timeLimitMs: 3000,
            memoryLimitMb: 128);

        submission.Id.Should().Be(submissionId);
        submission.InterviewQuestionId.Should().Be(interviewQuestionId);
        submission.Code.Should().Be("Console.WriteLine(3);");
        submission.LanguageCode.Should().Be("csharp");
        submission.TimeLimitMs.Should().Be(3000);
        submission.MemoryLimitMb.Should().Be(128);
        submission.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Create_ShouldSetDefaultState()
    {
        var submission = CodeSubmission.Create(
            id: Guid.NewGuid(),
            interviewQuestionId: Guid.NewGuid(),
            code: "print(3)",
            languageCode: "python",
            createdAtUtc: DateTime.UtcNow,
            timeLimitMs: 1000,
            memoryLimitMb: 64);

        submission.Status.Should().Be(ExecutionStatus.Pending);
        submission.OverallVerdict.Should().Be(Verdict.None);
        submission.ErrorMessage.Should().BeNull();
        submission.StartedAt.Should().BeNull();
        submission.CompletedAt.Should().BeNull();
        submission.TestCases.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldUseDefaultLimits_WhenLimitsAreNotProvided()
    {
        var submission = CodeSubmission.Create(
            id: Guid.NewGuid(),
            interviewQuestionId: Guid.NewGuid(),
            code: "print(3)",
            languageCode: "python",
            createdAtUtc: DateTime.UtcNow,
            timeLimitMs: null,
            memoryLimitMb: null);

        submission.TimeLimitMs.Should().Be(5000);
        submission.MemoryLimitMb.Should().Be(64);
    }

    [Fact]
    public void Create_ShouldAttachAndOrderTestCases()
    {
        var submissionId = Guid.NewGuid();
        var testCase2 = CodeSubmissionTestCase.Create(
            interviewTestCaseId: Guid.NewGuid(),
            orderIndex: 2,
            input: "2 2",
            expectedOutput: "4");
        var testCase1 = CodeSubmissionTestCase.Create(
            interviewTestCaseId: Guid.NewGuid(),
            orderIndex: 1,
            input: "1 2",
            expectedOutput: "3");

        var submission = CodeSubmission.Create(
            id: submissionId,
            interviewQuestionId: Guid.NewGuid(),
            code: "Console.WriteLine(3);",
            languageCode: "csharp",
            createdAtUtc: DateTime.UtcNow,
            timeLimitMs: 1000,
            memoryLimitMb: 64,
            testCases: [testCase2, testCase1]);

        submission.TestCases.Should().HaveCount(2);
        submission.TestCases.Select(x => x.OrderIndex).Should().ContainInOrder(1, 2);
        submission.TestCases.Should().OnlyContain(x => x.SubmissionId == submissionId);
        submission.TestCases[0].Should().BeSameAs(testCase1);
        submission.TestCases[1].Should().BeSameAs(testCase2);
    }
}
