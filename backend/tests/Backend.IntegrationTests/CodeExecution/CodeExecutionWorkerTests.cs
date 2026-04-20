using Backend.IntegrationTests.Infrastructure;
using Backend.IntegrationTests.Infrastructure.Fakes;
using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Workers;
using CodeExecution.UseCases.CodeSubmissions.Commands;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using CodeExecutionAppDbContext = CodeExecution.Infrastructure.Implementation.DataAccess.AppDbContext;
using ExecutionStage = CodeExecution.Infrastructure.Interfaces.CodeExecution.ExecutionStage;
using ExecutionStatus = CodeExecution.Domain.Enums.ExecutionStatus;
using CodeExecutionResult = CodeExecution.Infrastructure.Interfaces.CodeExecution.CodeExecutionResult;

namespace Backend.IntegrationTests.CodeExecution;

public sealed class CodeExecutionWorkerTests : BaseIntegrationTest
{
    public CodeExecutionWorkerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ProcessPendingSubmissionAsync_ShouldProcessOldestPendingSubmission()
    {
        await ClearCodeExecutionStateAsync();

        var fakeExecutor = ResetFakeCodeExecutor();

        var olderSubmissionId = await CreateSubmissionAsync(
        [
            CreateTestCase(orderIndex: 1, expectedOutput: "older")
        ]);

        var newerSubmissionId = await CreateSubmissionAsync(
        [
            CreateTestCase(orderIndex: 1, expectedOutput: "newer")
        ]);

        await SetCreatedAtAsync(olderSubmissionId, new DateTime(2026, 04, 15, 8, 0, 0, DateTimeKind.Utc));
        await SetCreatedAtAsync(newerSubmissionId, new DateTime(2026, 04, 15, 9, 0, 0, DateTimeKind.Utc));

        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = "older",
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 11,
            MemoryUsageMb = 7,
            Stage = ExecutionStage.Runtime
        });

        var processed = await CreateWorker().ProcessPendingSubmissionAsync(CancellationToken.None);

        var olderSubmission = await GetSubmissionAsync(olderSubmissionId);
        var newerSubmission = await GetSubmissionAsync(newerSubmissionId);

        processed.Should().BeTrue();

        olderSubmission.Should().NotBeNull();
        newerSubmission.Should().NotBeNull();

        olderSubmission.Status.Should().Be(ExecutionStatus.Completed);
        newerSubmission.Status.Should().Be(ExecutionStatus.Pending);
    }

    [Fact]
    public async Task ProcessPendingSubmissionAsync_ShouldSkipNonPendingSubmissions_AndProcessAvailablePendingSubmission()
    {
        await ClearCodeExecutionStateAsync();

        var fakeExecutor = ResetFakeCodeExecutor();

        var runningSubmissionId = await CreateSubmissionAsync(
        [
            CreateTestCase(orderIndex: 1, expectedOutput: "running")
        ]);

        var pendingSubmissionId = await CreateSubmissionAsync(
        [
            CreateTestCase(orderIndex: 1, expectedOutput: "pending")
        ]);

        await SetStatusAsync(runningSubmissionId, ExecutionStatus.Running);
        await SetCreatedAtAsync(runningSubmissionId, new DateTime(2026, 04, 15, 7, 0, 0, DateTimeKind.Utc));
        await SetCreatedAtAsync(pendingSubmissionId, new DateTime(2026, 04, 15, 8, 0, 0, DateTimeKind.Utc));

        fakeExecutor.EnqueueResult(new CodeExecutionResult
        {
            Output = "pending",
            Error = string.Empty,
            ExitCode = 0,
            TimeElapsedMs = 13,
            MemoryUsageMb = 9,
            Stage = ExecutionStage.Runtime
        });

        var processed = await CreateWorker().ProcessPendingSubmissionAsync(CancellationToken.None);

        var runningSubmission = await GetSubmissionAsync(runningSubmissionId);
        var pendingSubmission = await GetSubmissionAsync(pendingSubmissionId);

        processed.Should().BeTrue();

        runningSubmission.Should().NotBeNull();
        pendingSubmission.Should().NotBeNull();

        runningSubmission.Status.Should().Be(ExecutionStatus.Running);
        pendingSubmission.Status.Should().Be(ExecutionStatus.Completed);
    }

    [Fact]
    public async Task ProcessPendingSubmissionAsync_ShouldReturnFalse_WhenThereAreNoPendingSubmissions()
    {
        await ClearCodeExecutionStateAsync();

        var runningSubmissionId = await CreateSubmissionAsync(
        [
            CreateTestCase(orderIndex: 1, expectedOutput: "running")
        ]);

        var completedSubmissionId = await CreateSubmissionAsync(
        [
            CreateTestCase(orderIndex: 1, expectedOutput: "completed")
        ]);

        var failedSubmissionId = await CreateSubmissionAsync(
        [
            CreateTestCase(orderIndex: 1, expectedOutput: "failed")
        ]);

        await SetStatusAsync(runningSubmissionId, ExecutionStatus.Running);
        await SetStatusAsync(completedSubmissionId, ExecutionStatus.Completed);
        await SetStatusAsync(failedSubmissionId, ExecutionStatus.Failed);

        var processed = await CreateWorker().ProcessPendingSubmissionAsync(CancellationToken.None);

        var runningSubmission = await GetSubmissionAsync(runningSubmissionId);
        var completedSubmission = await GetSubmissionAsync(completedSubmissionId);
        var failedSubmission = await GetSubmissionAsync(failedSubmissionId);

        processed.Should().BeFalse();

        runningSubmission.Should().NotBeNull();
        completedSubmission.Should().NotBeNull();
        failedSubmission.Should().NotBeNull();

        runningSubmission.Status.Should().Be(ExecutionStatus.Running);
        completedSubmission.Status.Should().Be(ExecutionStatus.Completed);
        failedSubmission.Status.Should().Be(ExecutionStatus.Failed);
    }

    private CodeExecutionWorker CreateWorker()
    {
        return new CodeExecutionWorker(
            Factory.Services,
            NullLogger<CodeExecutionWorker>.Instance);
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

    private async Task ClearCodeExecutionStateAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM "CodeExecution"."CodeSubmissionTestCases";
            DELETE FROM "CodeExecution"."CodeSubmissions";
            DELETE FROM "CodeExecution"."OutboxMessages";
            """);
    }

    private async Task SetStatusAsync(Guid submissionId, ExecutionStatus status)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "CodeExecution"."CodeSubmissions" SET "Status" = {status.ToString()} WHERE "Id" = {submissionId}""");
    }

    private async Task SetCreatedAtAsync(Guid submissionId, DateTime createdAtUtc)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "CodeExecution"."CodeSubmissions" SET "CreatedAt" = {createdAtUtc} WHERE "Id" = {submissionId}""");
    }

    private async Task<CodeSubmission?> GetSubmissionAsync(Guid submissionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        return await db.CodeSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == submissionId);
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