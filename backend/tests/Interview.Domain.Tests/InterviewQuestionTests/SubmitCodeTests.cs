using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Domain.Models;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class SubmitCodeTests
{
    [Fact]
    public void SubmitCode_ShouldSetSubmitted_WhenQuestionIsEvaluatedCode()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = CreateCodingQuestion([testCase]);

        question.Start(DateTime.UtcNow.AddMinutes(-3));

        var draftSubmissionId = Guid.NewGuid();
        question.SubmitDraftCode("Console.WriteLine(3);", draftSubmissionId, DateTime.UtcNow.AddMinutes(-2));
        question.ApplyCodeSubmissionResult(
            draftSubmissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase.Id,
                    ActualOutput: "3",
                    ErrorMessage: null,
                    TimeElapsedMs: 10,
                    MemoryUsedMb: 8,
                    Verdict: Verdict.OK)
            ],
            overallVerdict: Verdict.OK,
            nowUtc: DateTime.UtcNow.AddMinutes(-1),
            errorMessage: null);

        var submittedAt = DateTime.UtcNow;

        var result = question.SubmitCode(submittedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Submitted);
        question.SubmittedAt.Should().Be(submittedAt);
        question.QuestionVerdict.Should().Be(QuestionVerdict.Correct);
        question.OverallVerdict.Should().Be(Verdict.OK);
        question.AiRetryCount.Should().Be(0);
        question.AiNextRetryAt.Should().BeNull();
    }

    [Fact]
    public void SubmitCode_ShouldReturnFailure_WhenQuestionTypeIsNotCoding()
    {
        var question = InterviewQuestion.Create(
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

        var result = question.SubmitCode(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_INVALID_TYPE");
        question.Status.Should().Be(QuestionStatus.NotStarted);
    }

    [Fact]
    public void SubmitCode_ShouldReturnFailure_WhenQuestionIsNotEvaluatedCode()
    {
        var question = CreateCodingQuestion();
        question.Start(DateTime.UtcNow.AddMinutes(-1));

        var result = question.SubmitCode(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CODE_NOT_EVALUATED");
        question.Status.Should().Be(QuestionStatus.InProgress);
        question.SubmittedAt.Should().BeNull();
    }

    private static InterviewQuestion CreateCodingQuestion(IReadOnlyCollection<TestCase>? testCases = null)
    {
        return InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: "Coding",
            text: "Solve task",
            type: QuestionType.Coding,
            orderIndex: 1,
            referenceSolution: "solution",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: "csharp",
            timeLimitMs: 1000,
            memoryLimitMb: 64,
            testCases: testCases);
    }
}
