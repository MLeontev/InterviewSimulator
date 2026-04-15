using CodeExecution.Domain.Entities;
using CodeExecution.Domain.Enums;
using FluentAssertions;

namespace CodeExecution.Domain.Tests.CodeSubmissionTestCaseTests;

public class CreateTests
{
    [Fact]
    public void Create_ShouldInitializeTestCaseWithProvidedValues()
    {
        var interviewTestCaseId = Guid.NewGuid();

        var testCase = CodeSubmissionTestCase.Create(
            interviewTestCaseId: interviewTestCaseId,
            orderIndex: 2,
            input: "1 2",
            expectedOutput: "3");

        testCase.Id.Should().NotBe(Guid.Empty);
        testCase.InterviewTestCaseId.Should().Be(interviewTestCaseId);
        testCase.OrderIndex.Should().Be(2);
        testCase.Input.Should().Be("1 2");
        testCase.ExpectedOutput.Should().Be("3");
    }

    [Fact]
    public void Create_ShouldSetDefaultExecutionState()
    {
        var testCase = CodeSubmissionTestCase.Create(
            interviewTestCaseId: Guid.NewGuid(),
            orderIndex: 1,
            input: "a b",
            expectedOutput: "ab");

        testCase.SubmissionId.Should().Be(Guid.Empty);
        testCase.ActualOutput.Should().BeNull();
        testCase.Error.Should().BeNull();
        testCase.ExitCode.Should().BeNull();
        testCase.TimeElapsedMs.Should().BeNull();
        testCase.MemoryUsedMb.Should().BeNull();
        testCase.Verdict.Should().Be(Verdict.None);
    }
}
