using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Domain.Models;

namespace Interview.Domain.Tests.InterviewQuestionTests;

public class ApplyCodeSubmissionResultTests
{
    [Fact]
    public void ApplyCodeSubmissionResult_ShouldApplyResults_WhenSubmissionMatches()
    {
        var testCase1 = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var testCase2 = TestCase.Create("2 2", "4", isHidden: true, orderIndex: 2);
        var question = CreateEvaluatingCodingQuestion([testCase1, testCase2], out var submissionId);
        var evaluatedAt = DateTime.UtcNow;

        var result = question.ApplyCodeSubmissionResult(
            submissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase1.Id,
                    ActualOutput: "3",
                    ErrorMessage: null,
                    TimeElapsedMs: 10,
                    MemoryUsedMb: 8,
                    Verdict: Verdict.OK),
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase2.Id,
                    ActualOutput: "5",
                    ErrorMessage: null,
                    TimeElapsedMs: 12,
                    MemoryUsedMb: 9,
                    Verdict: Verdict.WA)
            ],
            overallVerdict: Verdict.WA,
            nowUtc: evaluatedAt,
            errorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.EvaluatedCode);
        question.EvaluatedAt.Should().Be(evaluatedAt);
        question.OverallVerdict.Should().Be(Verdict.WA);
        question.QuestionVerdict.Should().Be(QuestionVerdict.PartiallyCorrect);
        question.ErrorMessage.Should().BeNull();

        question.TestCases[0].ActualOutput.Should().Be("3");
        question.TestCases[0].ExecutionTimeMs.Should().Be(10);
        question.TestCases[0].MemoryUsedMb.Should().Be(8);
        question.TestCases[0].Verdict.Should().Be(Verdict.OK);
        question.TestCases[0].ErrorMessage.Should().BeNull();

        question.TestCases[1].ActualOutput.Should().Be("5");
        question.TestCases[1].ExecutionTimeMs.Should().Be(12);
        question.TestCases[1].MemoryUsedMb.Should().Be(9);
        question.TestCases[1].Verdict.Should().Be(Verdict.WA);
        question.TestCases[1].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldSetDefaultSystemError_WhenOverallVerdictIsFailedSystem()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = CreateEvaluatingCodingQuestion([testCase], out var submissionId);

        var result = question.ApplyCodeSubmissionResult(
            submissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase.Id,
                    ActualOutput: string.Empty,
                    ErrorMessage: "runner failed",
                    TimeElapsedMs: 0,
                    MemoryUsedMb: 0,
                    Verdict: Verdict.FailedSystem)
            ],
            overallVerdict: Verdict.FailedSystem,
            nowUtc: DateTime.UtcNow,
            errorMessage: "");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.EvaluatedCode);
        question.OverallVerdict.Should().Be(Verdict.FailedSystem);
        question.ErrorMessage.Should().Be("Системная ошибка проверки кода");
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldUseProvidedSystemError_WhenOverallVerdictIsFailedSystem()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = CreateEvaluatingCodingQuestion([testCase], out var submissionId);

        var result = question.ApplyCodeSubmissionResult(
            submissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase.Id,
                    ActualOutput: string.Empty,
                    ErrorMessage: "runner failed",
                    TimeElapsedMs: 0,
                    MemoryUsedMb: 0,
                    Verdict: Verdict.FailedSystem)
            ],
            overallVerdict: Verdict.FailedSystem,
            nowUtc: DateTime.UtcNow,
            errorMessage: "docker unavailable");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.ErrorMessage.Should().Be("docker unavailable");
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldSetCorrectVerdict_WhenAllTestsPassed()
    {
        var testCase1 = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var testCase2 = TestCase.Create("2 2", "4", isHidden: true, orderIndex: 2);
        var question = CreateEvaluatingCodingQuestion([testCase1, testCase2], out var submissionId);

        var result = question.ApplyCodeSubmissionResult(
            submissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase1.Id,
                    ActualOutput: "3",
                    ErrorMessage: null,
                    TimeElapsedMs: 10,
                    MemoryUsedMb: 8,
                    Verdict: Verdict.OK),
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase2.Id,
                    ActualOutput: "4",
                    ErrorMessage: null,
                    TimeElapsedMs: 11,
                    MemoryUsedMb: 9,
                    Verdict: Verdict.OK)
            ],
            overallVerdict: Verdict.OK,
            nowUtc: DateTime.UtcNow,
            errorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.QuestionVerdict.Should().Be(QuestionVerdict.Correct);
        question.OverallVerdict.Should().Be(Verdict.OK);
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldSetIncorrectVerdict_WhenNoTestsPassed()
    {
        var testCase1 = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var testCase2 = TestCase.Create("2 2", "4", isHidden: true, orderIndex: 2);
        var question = CreateEvaluatingCodingQuestion([testCase1, testCase2], out var submissionId);

        var result = question.ApplyCodeSubmissionResult(
            submissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase1.Id,
                    ActualOutput: "5",
                    ErrorMessage: null,
                    TimeElapsedMs: 10,
                    MemoryUsedMb: 8,
                    Verdict: Verdict.WA),
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: testCase2.Id,
                    ActualOutput: string.Empty,
                    ErrorMessage: "runtime error",
                    TimeElapsedMs: 12,
                    MemoryUsedMb: 9,
                    Verdict: Verdict.RE)
            ],
            overallVerdict: Verdict.RE,
            nowUtc: DateTime.UtcNow,
            errorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.QuestionVerdict.Should().Be(QuestionVerdict.Incorrect);
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldSetIncorrectVerdict_WhenThereAreNoResults()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = CreateEvaluatingCodingQuestion([testCase], out var submissionId);

        var result = question.ApplyCodeSubmissionResult(
            submissionId,
            [],
            overallVerdict: Verdict.FailedSystem,
            nowUtc: DateTime.UtcNow,
            errorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        question.QuestionVerdict.Should().Be(QuestionVerdict.Incorrect);
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldReturnFalse_WhenSubmissionIdDoesNotMatch()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = CreateEvaluatingCodingQuestion([testCase], out _);

        var result = question.ApplyCodeSubmissionResult(
            Guid.NewGuid(),
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
            nowUtc: DateTime.UtcNow,
            errorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        question.Status.Should().Be(QuestionStatus.EvaluatingCode);
        question.EvaluatedAt.Should().BeNull();
        question.OverallVerdict.Should().Be(Verdict.None);
        question.TestCases[0].ActualOutput.Should().BeNull();
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldReturnFalse_WhenQuestionIsNotEvaluatingCode()
    {
        var testCase = TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1);
        var question = InterviewQuestion.Create(
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
            testCases: [testCase]);

        var result = question.ApplyCodeSubmissionResult(
            Guid.NewGuid(),
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
            nowUtc: DateTime.UtcNow,
            errorMessage: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        question.Status.Should().Be(QuestionStatus.NotStarted);
        question.EvaluatedAt.Should().BeNull();
    }

    [Fact]
    public void ApplyCodeSubmissionResult_ShouldReturnFailure_WhenQuestionIsNotCoding()
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

        var result = question.ApplyCodeSubmissionResult(
            Guid.NewGuid(),
            [],
            overallVerdict: Verdict.OK,
            nowUtc: DateTime.UtcNow,
            errorMessage: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUESTION_NOT_CODING");
    }

    private static InterviewQuestion CreateEvaluatingCodingQuestion(
        IReadOnlyCollection<TestCase> testCases,
        out Guid submissionId)
    {
        var question = InterviewQuestion.Create(
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

        question.Start(DateTime.UtcNow.AddMinutes(-1));
        submissionId = Guid.NewGuid();
        question.SubmitDraftCode("Console.WriteLine(3);", submissionId, DateTime.UtcNow);

        return question;
    }
}
