using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Domain.Models;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class SubmitDraftCodeTests
{
    [Fact]
    public void SubmitDraftCode_ShouldSetEvaluatingCode_WhenQuestionIsInProgress()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = CreateCodingQuestion("csharp", [testCase]);
        var startedAt = DateTime.UtcNow.AddMinutes(-1);
        var submittedAt = DateTime.UtcNow;
        var submissionId = Guid.NewGuid();

        question.Start(startedAt);

        var result = question.SubmitDraftCode("Console.WriteLine(3);", submissionId, submittedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.EvaluatingCode);
        question.Answer.Should().Be("Console.WriteLine(3);");
        question.SubmittedAt.Should().Be(submittedAt);
        question.EvaluatedAt.Should().BeNull();
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
        question.OverallVerdict.Should().Be(Verdict.None);
        question.AiFeedbackJson.Should().BeNull();
        question.ErrorMessage.Should().BeNull();
        question.LastSubmissionId.Should().Be(submissionId);

        question.TestCases[0].ActualOutput.Should().BeNull();
        question.TestCases[0].ExecutionTimeMs.Should().BeNull();
        question.TestCases[0].MemoryUsedMb.Should().BeNull();
        question.TestCases[0].Verdict.Should().Be(Verdict.None);
        question.TestCases[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SubmitDraftCode_ShouldResetPreviousEvaluation_WhenQuestionWasEvaluatedCode()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = CreateCodingQuestion("csharp", [testCase]);

        var startedAt = DateTime.UtcNow.AddMinutes(-3);
        var firstSubmittedAt = DateTime.UtcNow.AddMinutes(-2);
        var evaluatedAt = DateTime.UtcNow.AddMinutes(-1);
        var firstSubmissionId = Guid.NewGuid();

        question.Start(startedAt);
        question.SubmitDraftCode("Console.WriteLine(4);", firstSubmissionId, firstSubmittedAt);
        question.ApplyCodeSubmissionResult(
            firstSubmissionId,
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

        var secondSubmittedAt = DateTime.UtcNow;
        var secondSubmissionId = Guid.NewGuid();

        var result = question.SubmitDraftCode("Console.WriteLine(3);", secondSubmissionId, secondSubmittedAt);

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.EvaluatingCode);
        question.Answer.Should().Be("Console.WriteLine(3);");
        question.SubmittedAt.Should().Be(secondSubmittedAt);
        question.EvaluatedAt.Should().BeNull();
        question.QuestionVerdict.Should().Be(QuestionVerdict.None);
        question.OverallVerdict.Should().Be(Verdict.None);
        question.ErrorMessage.Should().BeNull();
        question.LastSubmissionId.Should().Be(secondSubmissionId);

        question.TestCases[0].ActualOutput.Should().BeNull();
        question.TestCases[0].ExecutionTimeMs.Should().BeNull();
        question.TestCases[0].MemoryUsedMb.Should().BeNull();
        question.TestCases[0].Verdict.Should().Be(Verdict.None);
        question.TestCases[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SubmitDraftCode_ShouldReturnFailure_WhenQuestionTypeIsNotCoding()
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

        var result = question.SubmitDraftCode("code", Guid.NewGuid(), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_CODING");
        question.Status.Should().Be(QuestionStatus.NotStarted);
    }

    [Fact]
    public void SubmitDraftCode_ShouldReturnFailure_WhenProgrammingLanguageIsNotSet()
    {
        var question = CreateCodingQuestion(programmingLanguageCode: null);
        question.Start(DateTime.UtcNow.AddMinutes(-1));

        var result = question.SubmitDraftCode("code", Guid.NewGuid(), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LANGUAGE_NOT_SET");
        question.Status.Should().Be(QuestionStatus.InProgress);
    }

    [Fact]
    public void SubmitDraftCode_ShouldReturnFailure_WhenQuestionAlreadyEvaluatingCode()
    {
        var question = CreateCodingQuestion("csharp");
        question.Start(DateTime.UtcNow.AddMinutes(-2));
        question.SubmitDraftCode("first code", Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1));

        var result = question.SubmitDraftCode("second code", Guid.NewGuid(), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CODE_CHECK_IN_PROGRESS");
        question.Answer.Should().Be("first code");
        question.Status.Should().Be(QuestionStatus.EvaluatingCode);
    }

    [Fact]
    public void SubmitDraftCode_ShouldReturnFailure_WhenStatusIsNotAllowed()
    {
        var question = CreateCodingQuestion("csharp");

        var result = question.SubmitDraftCode("code", Guid.NewGuid(), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_READY_FOR_DRAFT_SUBMIT");
        question.Status.Should().Be(QuestionStatus.NotStarted);
        question.Answer.Should().BeNull();
    }

    private static InterviewQuestion CreateCodingQuestion(
        string? programmingLanguageCode,
        IReadOnlyCollection<TestCase>? testCases = null)
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
            programmingLanguageCode: programmingLanguageCode,
            timeLimitMs: 1000,
            memoryLimitMb: 64,
            testCases: testCases);
    }
}
