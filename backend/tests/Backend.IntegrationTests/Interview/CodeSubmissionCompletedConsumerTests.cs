using Backend.IntegrationTests.Infrastructure;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using CodeExecutionCompletedEvent = CodeExecution.IntegrationEvents.CodeSubmissionCompleted;
using CodeExecutionTestCaseResultDto = CodeExecution.IntegrationEvents.TestCaseResultDto;
using ExecutionVerdict = CodeExecution.IntegrationEvents.Verdict;
using QuestionVerdict = Interview.Domain.Enums.QuestionVerdict;
using QuestionStatus = Interview.Domain.Enums.QuestionStatus;
using Verdict = Interview.Domain.Enums.Verdict;

namespace Backend.IntegrationTests.Interview;

public sealed class CodeSubmissionCompletedConsumerTests : InterviewIntegrationTestBase
{
    public CodeSubmissionCompletedConsumerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CodeSubmissionCompletedConsumer_ShouldApplyResultToMatchingSubmission()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await MakeCodingQuestionCurrentAsync(sessionId);
        var (questionId, submissionId) = await PutCodingQuestionIntoEvaluatingCodeAsync(sessionId);
        var questionBeforePublish = await GetQuestionAsync(questionId);

        questionBeforePublish.Should().NotBeNull();

        var completedEvent = BuildCompletedEvent(
            submissionId,
            questionId,
            questionBeforePublish.TestCases.ToList(),
            ExecutionVerdict.OK);

        using (var scope = CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            await bus.Publish(completedEvent);
        }

        await WaitForConditionAsync(async () =>
        {
            var question = await GetQuestionAsync(questionId);
            return question?.Status == QuestionStatus.EvaluatedCode;
        });

        var savedQuestion = await GetQuestionAsync(questionId);

        savedQuestion.Should().NotBeNull();
        savedQuestion.Status.Should().Be(QuestionStatus.EvaluatedCode);
        savedQuestion.OverallVerdict.Should().Be(Verdict.OK);
        savedQuestion.QuestionVerdict.Should().Be(QuestionVerdict.Correct);
        savedQuestion.ErrorMessage.Should().BeNull();
        savedQuestion.TestCases.Should().AllSatisfy(x =>
        {
            x.Verdict.Should().Be(Verdict.OK);
            x.ActualOutput.Should().Be(x.ExpectedOutput);
            x.ExecutionTimeMs.Should().NotBeNull();
            x.ExecutionTimeMs!.Value.Should().BeApproximately(11, 0.001);
            x.MemoryUsedMb.Should().NotBeNull();
            x.MemoryUsedMb!.Value.Should().BeApproximately(2, 0.001);
            x.ErrorMessage.Should().BeNull();
        });
    }

    [Fact]
    public async Task CodeSubmissionCompletedConsumer_ShouldIgnoreResult_WhenSubmissionIdIsStale()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await MakeCodingQuestionCurrentAsync(sessionId);
        var (questionId, _) = await PutCodingQuestionIntoEvaluatingCodeAsync(sessionId);
        var questionBeforePublish = await GetQuestionAsync(questionId);

        questionBeforePublish.Should().NotBeNull();

        var staleEvent = BuildCompletedEvent(
            Guid.NewGuid(),
            questionId,
            questionBeforePublish.TestCases.ToList(),
            ExecutionVerdict.OK);

        using (var scope = CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            await bus.Publish(staleEvent);
        }

        await Task.Delay(300);

        var savedQuestion = await GetQuestionAsync(questionId);

        savedQuestion.Should().NotBeNull();
        savedQuestion.Status.Should().Be(QuestionStatus.EvaluatingCode);
        savedQuestion.OverallVerdict.Should().Be(Verdict.None);
        savedQuestion.QuestionVerdict.Should().Be(QuestionVerdict.None);
        savedQuestion.ErrorMessage.Should().BeNull();
        savedQuestion.TestCases.Should().AllSatisfy(x =>
        {
            x.Verdict.Should().Be(Verdict.None);
            x.ActualOutput.Should().BeNull();
            x.ExecutionTimeMs.Should().BeNull();
            x.MemoryUsedMb.Should().BeNull();
            x.ErrorMessage.Should().BeNull();
        });
    }

    [Fact]
    public async Task CodeSubmissionCompletedConsumer_ShouldSaveSystemFailureResult_WhenExecutionFails()
    {
        using var userContext = await CreateAuthorizedCandidateAsync();

        var sessionId = await CreateSessionAsync(userContext);
        await MakeCodingQuestionCurrentAsync(sessionId);
        var (questionId, submissionId) = await PutCodingQuestionIntoEvaluatingCodeAsync(sessionId);
        var questionBeforePublish = await GetQuestionAsync(questionId);

        questionBeforePublish.Should().NotBeNull();

        var failedTestCase = questionBeforePublish.TestCases
            .OrderBy(x => x.OrderIndex)
            .First();

        var failedEvent = new CodeExecutionCompletedEvent(
            SubmissionId: submissionId,
            InterviewQuestionId: questionId,
            TestCaseResults:
            [
                new CodeExecutionTestCaseResultDto(
                    InterviewTestCaseId: failedTestCase.Id,
                    OrderIndex: failedTestCase.OrderIndex,
                    Input: failedTestCase.Input,
                    ExpectedOutput: failedTestCase.ExpectedOutput,
                    ActualOutput: string.Empty,
                    Error: "Sandbox unavailable",
                    ExitCode: 1,
                    TimeElapsedMs: 11,
                    MemoryUsedMb: 2,
                    Verdict: ExecutionVerdict.FailedSystem)
            ],
            OverallVerdict: ExecutionVerdict.FailedSystem,
            PassedCount: 0,
            TotalTests: questionBeforePublish.TestCases.Count,
            ErrorMessage: "Sandbox unavailable");

        using (var scope = CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            await bus.Publish(failedEvent);
        }

        await WaitForConditionAsync(async () =>
        {
            var question = await GetQuestionAsync(questionId);
            return question?.Status == QuestionStatus.EvaluatedCode;
        });

        var savedQuestion = await GetQuestionAsync(questionId);
        var firstSavedTestCase = savedQuestion!.TestCases.OrderBy(x => x.OrderIndex).First();
        var untouchedTestCases = savedQuestion.TestCases.OrderBy(x => x.OrderIndex).Skip(1).ToList();

        savedQuestion.Status.Should().Be(QuestionStatus.EvaluatedCode);
        savedQuestion.OverallVerdict.Should().Be(Verdict.FailedSystem);
        savedQuestion.QuestionVerdict.Should().Be(QuestionVerdict.Incorrect);
        savedQuestion.ErrorMessage.Should().Be("Sandbox unavailable");

        firstSavedTestCase.Verdict.Should().Be(Verdict.FailedSystem);
        firstSavedTestCase.ActualOutput.Should().BeEmpty();
        firstSavedTestCase.ExecutionTimeMs.Should().NotBeNull();
        firstSavedTestCase.ExecutionTimeMs!.Value.Should().BeApproximately(11, 0.001);
        firstSavedTestCase.MemoryUsedMb.Should().NotBeNull();
        firstSavedTestCase.MemoryUsedMb!.Value.Should().BeApproximately(2, 0.001);
        firstSavedTestCase.ErrorMessage.Should().Be("Sandbox unavailable");

        untouchedTestCases.Should().AllSatisfy(x =>
        {
            x.Verdict.Should().Be(Verdict.None);
            x.ActualOutput.Should().BeNull();
            x.ExecutionTimeMs.Should().BeNull();
            x.MemoryUsedMb.Should().BeNull();
            x.ErrorMessage.Should().BeNull();
        });
    }
}
