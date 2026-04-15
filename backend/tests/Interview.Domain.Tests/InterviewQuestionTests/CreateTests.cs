using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class CreateTests
{
    [Fact]
    public void Create_ShouldInitializeQuestionWithProvidedValues()
    {
        var sessionId = Guid.NewGuid();
        var competencyId = Guid.NewGuid();

        var question = InterviewQuestion.Create(
            sessionId: sessionId,
            title: "FizzBuzz",
            text: "Write a program",
            type: QuestionType.Coding,
            orderIndex: 3,
            referenceSolution: "solution",
            competencyId: competencyId,
            competencyName: "Algorithms",
            programmingLanguageCode: "python",
            timeLimitMs: 5000,
            memoryLimitMb: 128);

        question.Id.Should().NotBe(Guid.Empty);
        question.InterviewSessionId.Should().Be(sessionId);
        question.Title.Should().Be("FizzBuzz");
        question.Text.Should().Be("Write a program");
        question.Type.Should().Be(QuestionType.Coding);
        question.OrderIndex.Should().Be(3);
        question.ReferenceSolution.Should().Be("solution");
        question.CompetencyId.Should().Be(competencyId);
        question.CompetencyName.Should().Be("Algorithms");
        question.ProgrammingLanguageCode.Should().Be("python");
        question.TimeLimitMs.Should().Be(5000);
        question.MemoryLimitMb.Should().Be(128);
    }

    [Fact]
    public void Create_ShouldSetDefaultState()
    {
        var question = InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: "Question",
            text: "Text",
            type: QuestionType.Theory,
            orderIndex: 1,
            referenceSolution: "reference",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: null,
            timeLimitMs: null,
            memoryLimitMb: null);

        question.Status.Should().Be(QuestionStatus.NotStarted);
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
        question.OverallVerdict.Should().Be(Verdict.None);
        question.StartedAt.Should().BeNull();
        question.SubmittedAt.Should().BeNull();
        question.EvaluatedAt.Should().BeNull();
        question.Answer.Should().BeNull();
        question.AiFeedbackJson.Should().BeNull();
        question.ErrorMessage.Should().BeNull();
        question.AiRetryCount.Should().Be(0);
        question.AiNextRetryAt.Should().BeNull();
        question.LastSubmissionId.Should().BeNull();
        question.TestCases.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldAttachAndOrderTestCases()
    {
        var testCase2 = TestCase.Create("2", "4", isHidden: true, orderIndex: 2);
        var testCase1 = TestCase.Create("1", "2", isHidden: false, orderIndex: 1);

        var question = InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: "Sum",
            text: "Calculate",
            type: QuestionType.Coding,
            orderIndex: 1,
            referenceSolution: "reference",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: "csharp",
            timeLimitMs: 1000,
            memoryLimitMb: 64,
            testCases: [testCase2, testCase1]);

        question.TestCases.Should().HaveCount(2);
        question.TestCases.Select(x => x.OrderIndex).Should().ContainInOrder(1, 2);
        question.TestCases.Should().OnlyContain(x => x.InterviewQuestionId == question.Id);
        question.TestCases[0].Input.Should().Be("1");
        question.TestCases[1].Input.Should().Be("2");
    }
}
