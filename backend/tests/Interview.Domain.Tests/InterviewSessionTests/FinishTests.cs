using FluentAssertions;
using Interview.Domain.Entities;
using Interview.Domain.Enums;
using Interview.Domain.Models;

namespace Interview.Domain.Tests.InterviewSessionTests;

public class FinishTests
{
    [Fact]
    public void Finish_ShouldSetFinishedStatusAndFinishedAt_WhenSessionIsInProgress()
    {
        var session = CreateSession();
        var now = DateTime.UtcNow;

        var result = session.Finish(now);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(InterviewStatus.Finished);
        session.FinishedAt.Should().Be(now);
    }

    [Fact]
    public void Finish_ShouldMarkActiveQuestionsAsSkipped()
    {
        var notStartedQuestion = CreateTheoryQuestion(orderIndex: 1);
        var inProgressQuestion = CreateTheoryQuestion(orderIndex: 2);
        var evaluatedCodeQuestion = CreateCodingQuestion(orderIndex: 3);
        var submittedTheoryQuestion = CreateTheoryQuestion(orderIndex: 4);

        inProgressQuestion.Start(DateTime.UtcNow.AddMinutes(-3));

        evaluatedCodeQuestion.Start(DateTime.UtcNow.AddMinutes(-4));
        var submissionId = Guid.NewGuid();
        var codingTestCase = evaluatedCodeQuestion.TestCases[0];
        evaluatedCodeQuestion.SubmitDraftCode("Console.WriteLine(3);", submissionId, DateTime.UtcNow.AddMinutes(-2));
        evaluatedCodeQuestion.ApplyCodeSubmissionResult(
            submissionId,
            [
                new CodeCheckTestCaseResult(
                    InterviewTestCaseId: codingTestCase.Id,
                    ActualOutput: "3",
                    ErrorMessage: null,
                    TimeElapsedMs: 10,
                    MemoryUsedMb: 8,
                    Verdict: Verdict.OK)
            ],
            overallVerdict: Verdict.OK,
            nowUtc: DateTime.UtcNow.AddMinutes(-1),
            errorMessage: null);

        submittedTheoryQuestion.Start(DateTime.UtcNow.AddMinutes(-2));
        submittedTheoryQuestion.SubmitTheoryAnswer("answer", DateTime.UtcNow.AddMinutes(-1));

        var session = CreateSession([submittedTheoryQuestion, evaluatedCodeQuestion, inProgressQuestion, notStartedQuestion]);

        var result = session.Finish(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        notStartedQuestion.Status.Should().Be(QuestionStatus.NotStarted);
        inProgressQuestion.Status.Should().Be(QuestionStatus.Skipped);
        evaluatedCodeQuestion.Status.Should().Be(QuestionStatus.Skipped);
        submittedTheoryQuestion.Status.Should().Be(QuestionStatus.Submitted);
    }

    [Fact]
    public void Finish_ShouldReturnFailure_WhenSessionIsNotInProgress()
    {
        var session = CreateSession();
        session.Finish(DateTime.UtcNow);

        var result = session.Finish(DateTime.UtcNow.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SESSION_CANNOT_BE_FINISHED");
        session.Status.Should().Be(InterviewStatus.Finished);
    }

    private static InterviewSession CreateSession(IReadOnlyCollection<InterviewQuestion>? questions = null)
    {
        return InterviewSession.Create(
            id: Guid.NewGuid(),
            candidateId: Guid.NewGuid(),
            interviewPresetId: Guid.NewGuid(),
            interviewPresetName: "Preset",
            startedAt: DateTime.UtcNow.AddHours(-1),
            plannedEndAt: DateTime.UtcNow.AddHours(1),
            questions: questions);
    }

    private static InterviewQuestion CreateTheoryQuestion(int orderIndex)
    {
        return InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: $"Theory {orderIndex}",
            text: "Explain something",
            type: QuestionType.Theory,
            orderIndex: orderIndex,
            referenceSolution: "reference",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: null,
            timeLimitMs: null,
            memoryLimitMb: null);
    }

    private static InterviewQuestion CreateCodingQuestion(int orderIndex)
    {
        return InterviewQuestion.Create(
            sessionId: Guid.NewGuid(),
            title: $"Coding {orderIndex}",
            text: "Solve task",
            type: QuestionType.Coding,
            orderIndex: orderIndex,
            referenceSolution: "solution",
            competencyId: null,
            competencyName: null,
            programmingLanguageCode: "csharp",
            timeLimitMs: 1000,
            memoryLimitMb: 64,
            testCases: [TestCase.Create("1 2", "3", isHidden: false, orderIndex: 1)]);
    }
}
