using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.TestCaseTests;

public class CreateTests
{
    [Fact]
    public void Create_ShouldInitializeTestCaseWithProvidedValues()
    {
        var testCase = TestCase.Create(
            input: "1 2",
            expectedOutput: "3",
            isHidden: true,
            orderIndex: 2);

        testCase.Id.Should().NotBe(Guid.Empty);
        testCase.Input.Should().Be("1 2");
        testCase.ExpectedOutput.Should().Be("3");
        testCase.IsHidden.Should().BeTrue();
        testCase.OrderIndex.Should().Be(2);
    }

    [Fact]
    public void Create_ShouldSetDefaultExecutionState()
    {
        var testCase = TestCase.Create(
            input: "a b",
            expectedOutput: "ab",
            isHidden: false,
            orderIndex: 1);

        testCase.InterviewQuestionId.Should().Be(Guid.Empty);
        testCase.ActualOutput.Should().BeNull();
        testCase.ExecutionTimeMs.Should().BeNull();
        testCase.MemoryUsedMb.Should().BeNull();
        testCase.Verdict.Should().Be(Verdict.None);
        testCase.ErrorMessage.Should().BeNull();
    }
}
