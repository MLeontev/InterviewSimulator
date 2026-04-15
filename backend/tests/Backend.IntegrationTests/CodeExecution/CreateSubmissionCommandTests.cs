using Backend.IntegrationTests.Infrastructure;
using CodeExecution.Domain.Entities;
using CodeExecution.UseCases.Commands;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CodeExecutionAppDbContext = CodeExecution.Infrastructure.Implementation.DataAccess.AppDbContext;
using ExecutionStatus = CodeExecution.Domain.Enums.ExecutionStatus;
using Verdict = CodeExecution.Domain.Enums.Verdict;

namespace Backend.IntegrationTests.CodeExecution;

public sealed class CreateSubmissionCommandTests : BaseIntegrationTest
{
    public CreateSubmissionCommandTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateSubmissionCommand_ShouldPersistPendingSubmission_WhenRequestIsValid()
    {
        var submissionId = Guid.NewGuid();
        var interviewQuestionId = Guid.NewGuid();

        await Sender.Send(new CreateSubmissionCommand(
            SubmissionId: submissionId,
            InterviewQuestionId: interviewQuestionId,
            Code: "print('hello')",
            LanguageCode: "python",
            TestCases:
            [
                CreateTestCase(orderIndex: 3, expectedOutput: "third"),
                CreateTestCase(orderIndex: 1, expectedOutput: "first"),
                CreateTestCase(orderIndex: 2, expectedOutput: "second")
            ],
            TimeLimitMs: 2_000,
            MemoryLimitMb: 128));

        var submission = await GetSubmissionAsync(submissionId);

        submission.Should().NotBeNull();
        var savedSubmission = submission!;

        savedSubmission.InterviewQuestionId.Should().Be(interviewQuestionId);
        savedSubmission.Code.Should().Be("print('hello')");
        savedSubmission.LanguageCode.Should().Be("python");
        savedSubmission.TimeLimitMs.Should().Be(2_000);
        savedSubmission.MemoryLimitMb.Should().Be(128);
        savedSubmission.Status.Should().Be(ExecutionStatus.Pending);
        savedSubmission.OverallVerdict.Should().Be(Verdict.None);
        savedSubmission.StartedAt.Should().BeNull();
        savedSubmission.CompletedAt.Should().BeNull();

        var testCases = savedSubmission.TestCases.OrderBy(x => x.OrderIndex).ToList();
        testCases.Select(x => x.OrderIndex).Should().Equal(1, 2, 3);
        testCases.Select(x => x.ExpectedOutput).Should().Equal("first", "second", "third");
        testCases.Should().OnlyContain(x => x.Verdict == Verdict.None);
    }

    [Fact]
    public async Task CreateSubmissionCommand_ShouldUseDefaultLimits_WhenRequestDoesNotProvideThem()
    {
        var submissionId = Guid.NewGuid();

        await Sender.Send(new CreateSubmissionCommand(
            SubmissionId: submissionId,
            InterviewQuestionId: Guid.NewGuid(),
            Code: "print('hello')",
            LanguageCode: "python",
            TestCases:
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "hello")
            ]));

        var submission = await GetSubmissionAsync(submissionId);

        submission.Should().NotBeNull();
        var savedSubmission = submission!;

        savedSubmission.TimeLimitMs.Should().Be(5_000);
        savedSubmission.MemoryLimitMb.Should().Be(64);
    }

    [Fact]
    public async Task CreateSubmissionCommand_ShouldBeIdempotent_WhenSubmissionAlreadyExists()
    {
        var submissionId = Guid.NewGuid();

        await Sender.Send(new CreateSubmissionCommand(
            SubmissionId: submissionId,
            InterviewQuestionId: Guid.NewGuid(),
            Code: "print('first')",
            LanguageCode: "python",
            TestCases:
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "first")
            ],
            TimeLimitMs: 1_500,
            MemoryLimitMb: 96));

        await Sender.Send(new CreateSubmissionCommand(
            SubmissionId: submissionId,
            InterviewQuestionId: Guid.NewGuid(),
            Code: "print('second')",
            LanguageCode: "python",
            TestCases:
            [
                CreateTestCase(orderIndex: 1, expectedOutput: "second"),
                CreateTestCase(orderIndex: 2, expectedOutput: "second-2")
            ],
            TimeLimitMs: 9_999,
            MemoryLimitMb: 512));

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        var submissions = await db.CodeSubmissions
            .Include(x => x.TestCases)
            .Where(x => x.Id == submissionId)
            .ToListAsync();

        submissions.Should().ContainSingle();

        var savedSubmission = submissions.Single();
        savedSubmission.Code.Should().Be("print('first')");
        savedSubmission.TimeLimitMs.Should().Be(1_500);
        savedSubmission.MemoryLimitMb.Should().Be(96);
        savedSubmission.TestCases.Should().ContainSingle();
        savedSubmission.TestCases.Single().ExpectedOutput.Should().Be("first");
    }

    private async Task<CodeSubmission?> GetSubmissionAsync(Guid submissionId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

        return await db.CodeSubmissions
            .Include(x => x.TestCases)
            .SingleOrDefaultAsync(x => x.Id == submissionId);
    }

    private static CreateSubmissionTestCaseDto CreateTestCase(int orderIndex, string expectedOutput, string input = "input")
        => new(Guid.NewGuid(), orderIndex, input, expectedOutput);
}
