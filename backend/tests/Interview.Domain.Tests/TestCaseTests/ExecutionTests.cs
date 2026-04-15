using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.TestCaseTests;

public class ExecutionTests
{
    [Fact]
    public void ApplyExecutionResult_ShouldSetExecutionFields()
    {
        var testCase = CreateTestCase();

        testCase.ApplyExecutionResult(
            actualOutput: "4",
            timeElapsedMs: 12.5,
            memoryUsedMb: 8.2,
            verdict: Verdict.WA,
            errorMessage: "wrong answer");

        testCase.ActualOutput.Should().Be("4");
        testCase.ExecutionTimeMs.Should().Be(12.5);
        testCase.MemoryUsedMb.Should().Be(8.2);
        testCase.Verdict.Should().Be(Verdict.WA);
        testCase.ErrorMessage.Should().Be("wrong answer");
    }

    [Fact]
    public void ApplyExecutionResult_ShouldSetErrorMessageNull_WhenErrorIsWhitespace()
    {
        var testCase = CreateTestCase();

        testCase.ApplyExecutionResult(
            actualOutput: "3",
            timeElapsedMs: 10,
            memoryUsedMb: 7,
            verdict: Verdict.OK,
            errorMessage: "   ");

        testCase.ErrorMessage.Should().BeNull();
        testCase.Verdict.Should().Be(Verdict.OK);
    }

    [Fact]
    public void Reset_ShouldClearExecutionState()
    {
        var testCase = CreateTestCase();

        testCase.ApplyExecutionResult(
            actualOutput: "runtime error",
            timeElapsedMs: 25,
            memoryUsedMb: 16,
            verdict: Verdict.RE,
            errorMessage: "stack overflow");

        testCase.Reset();

        testCase.ActualOutput.Should().BeNull();
        testCase.ExecutionTimeMs.Should().BeNull();
        testCase.MemoryUsedMb.Should().BeNull();
        testCase.Verdict.Should().Be(Verdict.None);
        testCase.ErrorMessage.Should().BeNull();
    }

    private static TestCase CreateTestCase()
    {
        return TestCase.Create(
            input: "1 2",
            expectedOutput: "3",
            isHidden: false,
            orderIndex: 1);
    }
}
