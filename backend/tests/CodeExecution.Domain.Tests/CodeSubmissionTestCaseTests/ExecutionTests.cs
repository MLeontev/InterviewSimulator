using CodeExecution.Domain.Entities;
using CodeExecution.Domain.Enums;
using FluentAssertions;

namespace CodeExecution.Domain.Tests.CodeSubmissionTestCaseTests;

public class ExecutionTests
{
    [Fact]
    public void ApplyExecutionResult_ShouldSetExecutionFields()
    {
        var testCase = CreateTestCase();

        testCase.ApplyExecutionResult(
            actualOutput: "4",
            error: "wrong answer",
            exitCode: 1,
            timeElapsedMs: 12.5,
            memoryUsedMb: 8.2,
            verdict: Verdict.WA);

        testCase.ActualOutput.Should().Be("4");
        testCase.Error.Should().Be("wrong answer");
        testCase.ExitCode.Should().Be(1);
        testCase.TimeElapsedMs.Should().Be(12.5);
        testCase.MemoryUsedMb.Should().Be(8.2);
        testCase.Verdict.Should().Be(Verdict.WA);
    }

    [Fact]
    public void ApplyExecutionResult_ShouldSetErrorNull_WhenErrorIsWhitespace()
    {
        var testCase = CreateTestCase();

        testCase.ApplyExecutionResult(
            actualOutput: "3",
            error: "   ",
            exitCode: 0,
            timeElapsedMs: 10,
            memoryUsedMb: 7,
            verdict: Verdict.OK);

        testCase.ActualOutput.Should().Be("3");
        testCase.Error.Should().BeNull();
        testCase.ExitCode.Should().Be(0);
        testCase.TimeElapsedMs.Should().Be(10);
        testCase.MemoryUsedMb.Should().Be(7);
        testCase.Verdict.Should().Be(Verdict.OK);
    }

    private static CodeSubmissionTestCase CreateTestCase()
    {
        return CodeSubmissionTestCase.Create(
            interviewTestCaseId: Guid.NewGuid(),
            orderIndex: 1,
            input: "1 2",
            expectedOutput: "3");
    }
}
