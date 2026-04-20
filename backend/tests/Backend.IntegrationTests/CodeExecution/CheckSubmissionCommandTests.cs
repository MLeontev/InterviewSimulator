using System.Text.Json;
using Backend.IntegrationTests.Infrastructure;
using Backend.IntegrationTests.Infrastructure.Fakes;
using CodeExecution.Domain.Entities;
using CodeExecution.IntegrationEvents;
using CodeExecution.UseCases.CodeSubmissions.Commands;
using FluentAssertions;
using Framework.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CodeExecutionResult = CodeExecution.Infrastructure.Interfaces.CodeExecution.CodeExecutionResult;
using CodeExecutionAppDbContext = CodeExecution.Infrastructure.Implementation.DataAccess.AppDbContext;
using ExecutionStage = CodeExecution.Infrastructure.Interfaces.CodeExecution.ExecutionStage;
using ExecutionStatus = CodeExecution.Domain.Enums.ExecutionStatus;
using DomainVerdict = CodeExecution.Domain.Enums.Verdict;

namespace Backend.IntegrationTests.CodeExecution;

public sealed class CheckSubmissionCommandTests : BaseIntegrationTest
{
    public CheckSubmissionCommandTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CheckSubmissionCommand_ShouldIgnoreSubmission_WhenStatusIsNotRunning()
    {
        var fakeExecutor = ResetFakeCodeExecutor();
        var submissionId = await CreateSubmissionAsync(
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "hello")
            ]);

        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = "hello",
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 10,
            MemoryUsageMb = 1,
            Stage = ExecutionStage.Runtime
        });

        await SendAsync(new CheckSubmissionCommand(submissionId));

        var submission = await GetSubmissionAsync(submissionId);
        var outboxMessage = await FindOutboxMessageAsync(submissionId);

        submission.Should().NotBeNull();

        submission.Status.Should().Be(ExecutionStatus.Pending);
        submission.StartedAt.Should().BeNull();
        submission.CompletedAt.Should().BeNull();
        submission.OverallVerdict.Should().Be(DomainVerdict.None);
        submission.TestCases.Should().OnlyContain(x => x.Verdict == DomainVerdict.None);
        outboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task CheckSubmissionCommand_ShouldCompleteSubmissionAndWriteOutbox_WhenAllTestsPass()
    {
        var fakeExecutor = ResetFakeCodeExecutor();
        var submissionId = await CreateRunningSubmissionAsync(
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "first"),
                CreateTestCase(orderIndex: 2, expectedOutput: "second")
            ]);

        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = "first \n",
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 10,
            MemoryUsageMb = 12,
            Stage = ExecutionStage.Runtime
        });
        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = "second",
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 15,
            MemoryUsageMb = 18,
            Stage = ExecutionStage.Runtime
        });

        await SendAsync(new CheckSubmissionCommand(submissionId));

        var submission = await GetSubmissionAsync(submissionId);
        var outboxMessage = await FindOutboxMessageAsync(submissionId);
        var payload = DeserializeOutboxPayload(outboxMessage);

        submission.Should().NotBeNull();
        outboxMessage.Should().NotBeNull();
        payload.Should().NotBeNull();

        submission.Status.Should().Be(ExecutionStatus.Completed);
        submission.OverallVerdict.Should().Be(DomainVerdict.OK);
        submission.ErrorMessage.Should().BeNull();
        submission.StartedAt.Should().NotBeNull();
        submission.CompletedAt.Should().NotBeNull();

        var testCases = submission.TestCases.OrderBy(x => x.OrderIndex).ToList();
        testCases.Should().OnlyContain(x => x.Verdict == DomainVerdict.OK);
        testCases.Select(x => x.ActualOutput).Should().Equal("first", "second");

        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.Type.Should().Contain(nameof(CodeSubmissionCompleted));

        payload.SubmissionId.Should().Be(submissionId);
        payload.OverallVerdict.Should().Be(Verdict.OK);
        payload.PassedCount.Should().Be(2);
        payload.TotalTests.Should().Be(2);
        payload.ErrorMessage.Should().BeNull();
        payload.TestCaseResults.Select(x => x.OrderIndex).Should().Equal(1, 2);
        payload.TestCaseResults.Should().OnlyContain(x => x.Verdict == Verdict.OK);
    }

    [Fact]
    public async Task CheckSubmissionCommand_ShouldStopAfterFirstFailedTest_AndPublishOnlyProcessedResults()
    {
        var fakeExecutor = ResetFakeCodeExecutor();
        var submissionId = await CreateRunningSubmissionAsync(
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "first"),
                CreateTestCase(orderIndex: 2, expectedOutput: "second"),
                CreateTestCase(orderIndex: 3, expectedOutput: "third")
            ]);

        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = "first",
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 5,
            MemoryUsageMb = 10,
            Stage = ExecutionStage.Runtime
        });
        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = "wrong",
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 7,
            MemoryUsageMb = 11,
            Stage = ExecutionStage.Runtime
        });

        await SendAsync(new CheckSubmissionCommand(submissionId));

        var submission = await GetSubmissionAsync(submissionId);
        var payload = DeserializeOutboxPayload(await FindOutboxMessageAsync(submissionId));

        submission.Should().NotBeNull();
        payload.Should().NotBeNull();

        submission.Status.Should().Be(ExecutionStatus.Completed);
        submission.OverallVerdict.Should().Be(DomainVerdict.WA);

        var testCases = submission.TestCases.OrderBy(x => x.OrderIndex).ToList();
        testCases[0].Verdict.Should().Be(DomainVerdict.OK);
        testCases[1].Verdict.Should().Be(DomainVerdict.WA);
        testCases[2].Verdict.Should().Be(DomainVerdict.None);
        testCases[2].ActualOutput.Should().BeNull();

        payload.OverallVerdict.Should().Be(Verdict.WA);
        payload.PassedCount.Should().Be(1);
        payload.TotalTests.Should().Be(3);
        payload.TestCaseResults.Should().HaveCount(2);
        payload.TestCaseResults.Select(x => x.OrderIndex).Should().Equal(1, 2);
        payload.TestCaseResults.Last().Verdict.Should().Be(Verdict.WA);
    }

    [Fact]
    public async Task CheckSubmissionCommand_ShouldMarkSubmissionAsCompletedWithRuntimeError_WhenExecutionFails()
    {
        var fakeExecutor = ResetFakeCodeExecutor();
        var submissionId = await CreateRunningSubmissionAsync(
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "expected")
            ]);

        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = string.Empty,
            Error = "runtime failure",
            ExitCode = 1,
            TimeElapsedMs = 6,
            MemoryUsageMb = 9,
            Stage = ExecutionStage.Runtime
        });

        await SendAsync(new CheckSubmissionCommand(submissionId));

        var submission = await GetSubmissionAsync(submissionId);
        var payload = DeserializeOutboxPayload(await FindOutboxMessageAsync(submissionId));

        submission.Should().NotBeNull();
        payload.Should().NotBeNull();

        submission.Status.Should().Be(ExecutionStatus.Completed);
        submission.OverallVerdict.Should().Be(DomainVerdict.RE);
        submission.ErrorMessage.Should().BeNull();
        submission.TestCases.Single().Verdict.Should().Be(DomainVerdict.RE);
        submission.TestCases.Single().Error.Should().Be("runtime failure");

        payload.OverallVerdict.Should().Be(Verdict.RE);
        payload.ErrorMessage.Should().BeNull();
        payload.TestCaseResults.Should().ContainSingle();
        payload.TestCaseResults.Single().Verdict.Should().Be(Verdict.RE);
        payload.TestCaseResults.Single().Error.Should().Be("runtime failure");
    }

    [Fact]
    public async Task CheckSubmissionCommand_ShouldMarkSubmissionAsFailed_WhenExecutorConfigurationIsMissing()
    {
        var fakeExecutor = ResetFakeCodeExecutor();
        var submissionId = await CreateRunningSubmissionAsync(
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "expected")
            ]);

        fakeExecutor.EnqueueException(new KeyNotFoundException("Unsupported language"));

        await SendAsync(new CheckSubmissionCommand(submissionId));

        var submission = await GetSubmissionAsync(submissionId);
        var outboxMessage = await FindOutboxMessageAsync(submissionId);
        var payload = DeserializeOutboxPayload(outboxMessage);

        submission.Should().NotBeNull();
        outboxMessage.Should().NotBeNull();
        payload.Should().NotBeNull();

        submission.Status.Should().Be(ExecutionStatus.Failed);
        submission.OverallVerdict.Should().Be(DomainVerdict.FailedSystem);
        submission.ErrorMessage.Should().Be("Конфигурация для языка программирования не найдена");
        submission.TestCases.Single().Verdict.Should().Be(DomainVerdict.None);

        outboxMessage.ProcessedAt.Should().BeNull();

        payload.OverallVerdict.Should().Be(Verdict.FailedSystem);
        payload.PassedCount.Should().Be(0);
        payload.TotalTests.Should().Be(1);
        payload.ErrorMessage.Should().Be("Конфигурация для языка программирования не найдена");
    }

    private FakeCodeExecutor ResetFakeCodeExecutor()
    {
        var fakeExecutor = Factory.GetFakeCodeExecutor();
        fakeExecutor.Reset();
        return fakeExecutor;
    }

    private async Task<Guid> CreateSubmissionAsync(
        IReadOnlyList<CreateSubmissionTestCaseDto> testCases,
        string code = "print('hello')",
        string languageCode = "python",
        int? timeLimitMs = 2_000,
        int? memoryLimitMb = 128)
    {
        var submissionId = Guid.NewGuid();

        await SendAsync(new CreateSubmissionCommand(
            SubmissionId: submissionId,
            InterviewQuestionId: Guid.NewGuid(),
            Code: code,
            LanguageCode: languageCode,
            TestCases: testCases,
            TimeLimitMs: timeLimitMs,
            MemoryLimitMb: memoryLimitMb));

        return submissionId;
    }

    private async Task<Guid> CreateRunningSubmissionAsync(
        IReadOnlyList<CreateSubmissionTestCaseDto> testCases,
        string code = "print('hello')",
        string languageCode = "python",
        int? timeLimitMs = 2_000,
        int? memoryLimitMb = 128)
    {
        var submissionId = await CreateSubmissionAsync(testCases, code, languageCode, timeLimitMs, memoryLimitMb);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "CodeExecution"."CodeSubmissions" SET "Status" = {nameof(ExecutionStatus.Running)} WHERE "Id" = {submissionId}""");

        return submissionId;
    }

    private async Task<CodeSubmission?> GetSubmissionAsync(Guid submissionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        return await db.CodeSubmissions
            .Include(x => x.TestCases)
            .SingleOrDefaultAsync(x => x.Id == submissionId);
    }

    private async Task<OutboxMessage?> FindOutboxMessageAsync(Guid submissionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        var messages = await db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Type.Contains(nameof(CodeSubmissionCompleted)))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        foreach (var message in messages)
        {
            var payload = DeserializeOutboxPayload(message);
            if (payload?.SubmissionId == submissionId)
                return message;
        }

        return null;
    }

    private static CodeSubmissionCompleted? DeserializeOutboxPayload(OutboxMessage? message)
    {
        if (message is null)
            return null;

        return JsonSerializer.Deserialize<CodeSubmissionCompleted>(message.Payload);
    }

    private async Task SendAsync<TRequest>(TRequest request)
        where TRequest : class, MediatR.IRequest
    {
        using var scope = CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        await sender.Send(request);
    }

    private static CreateSubmissionTestCaseDto CreateTestCase(int orderIndex, string expectedOutput, string input = "input")
        => new(Guid.NewGuid(), orderIndex, input, expectedOutput);
}
