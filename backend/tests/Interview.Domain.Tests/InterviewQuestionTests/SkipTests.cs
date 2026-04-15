using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Domain.Models;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class SkipTests
{
    [Fact]
    public void Skip_ShouldSetSkipped_WhenStatusIsNotStarted()
    {
        var question = CreateTheoryQuestion();

        var result = question.Skip();

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Skipped);
        question.Answer.Should().BeNull();
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
        question.OverallVerdict.Should().Be(Verdict.None);
    }

    [Fact]
    public void Skip_ShouldSetSkipped_WhenStatusIsInProgress()
    {
        var question = CreateTheoryQuestion();
        question.Start(DateTime.UtcNow);

        var result = question.Skip();

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Skipped);
    }

    [Fact]
    public void Skip_ShouldResetStateAndTestCases_WhenStatusIsEvaluatedCode()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: "Sum",
            text: "Calculate sum",
            type: QuestionType.Coding,
            orderIndex: 1,
            referenceSolution: "return a + b;",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: "csharp",
            timeLimitMs: 1000,
            memoryLimitMb: 64,
            testCases: [testCase]);

        var startedAt = DateTime.UtcNow.AddMinutes(-3);
        var submittedAt = DateTime.UtcNow.AddMinutes(-2);
        var evaluatedAt = DateTime.UtcNow.AddMinutes(-1);
        var submissionId = Guid.NewGuid();

        question.Start(startedAt);
        question.SubmitDraftCode("Console.WriteLine(3);", submissionId, submittedAt);
        question.ApplyCodeSubmissionResult(
            submissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase.Id,
                    ActualOutput: "4",
                    ErrorMessage: null,
                    TimeElapsedMs: 12,
                    MemoryUsedMb: 8,
                    Verdict: Verdict.WA)
            ],
            overallVerdict: Verdict.WA,
            nowUtc: evaluatedAt,
            errorMessage: null);

        var result = question.Skip();

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Skipped);
        question.Answer.Should().BeNull();
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
        question.OverallVerdict.Should().Be(Verdict.None);
        question.AiFeedbackJson.Should().BeNull();
        question.ErrorMessage.Should().BeNull();
        question.SubmittedAt.Should().BeNull();
        question.EvaluatedAt.Should().BeNull();

        question.TestCases.Should().HaveCount(1);
        question.TestCases[0].ActualOutput.Should().BeNull();
        question.TestCases[0].ExecutionTimeMs.Should().BeNull();
        question.TestCases[0].MemoryUsedMb.Should().BeNull();
        question.TestCases[0].Verdict.Should().Be(Verdict.None);
        question.TestCases[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Skip_ShouldReturnFailure_WhenStatusIsSubmitted()
    {
        var question = CreateTheoryQuestion();
        question.Start(DateTime.UtcNow.AddMinutes(-1));
        question.SubmitTheoryAnswer("answer", DateTime.UtcNow);

        var result = question.Skip();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_CANNOT_BE_SKIPPED");
        question.Status.Should().Be(QuestionStatus.Submitted);
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
