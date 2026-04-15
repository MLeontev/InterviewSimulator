using Backend.IntegrationTests.Infrastructure;
using CodeExecution.Domain.Entities;
using FluentAssertions;
using Interview.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CodeExecutionAppDbContext = CodeExecution.Infrastructure.Implementation.DataAccess.AppDbContext;
using ExecutionStatus = CodeExecution.Domain.Enums.ExecutionStatus;
using Verdict = CodeExecution.Domain.Enums.Verdict;

namespace Backend.IntegrationTests.CodeExecution;

public sealed class CodeSubmissionCreatedConsumerTests : BaseIntegrationTest
{
    public CodeSubmissionCreatedConsumerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CodeSubmissionCreatedConsumer_ShouldCreatePendingSubmission_WhenEventIsPublished()
    {
        var submissionId = Guid.NewGuid();
        var interviewQuestionId = Guid.NewGuid();

        using (var scope = CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();

            await bus.Publish(new CodeSubmissionCreated(
                SubmissionId: submissionId,
                InterviewQuestionId: interviewQuestionId,
                Code: "print('hello')",
                LanguageCode: "python",
                TestCases:
                [
                    new CodeSubmissionCreatedTestCase(Guid.NewGuid(), 2, "input-2", "second"),
                    new CodeSubmissionCreatedTestCase(Guid.NewGuid(), 1, "input-1", "first")
                ],
                TimeLimitMs: 2_500,
                MemoryLimitMb: 192));
        }

        var submission = await WaitForSubmissionAsync(submissionId);

        submission.Should().NotBeNull();
        var savedSubmission = submission!;

        savedSubmission.InterviewQuestionId.Should().Be(interviewQuestionId);
        savedSubmission.Code.Should().Be("print('hello')");
        savedSubmission.LanguageCode.Should().Be("python");
        savedSubmission.TimeLimitMs.Should().Be(2_500);
        savedSubmission.MemoryLimitMb.Should().Be(192);
        savedSubmission.Status.Should().Be(ExecutionStatus.Pending);
        savedSubmission.OverallVerdict.Should().Be(Verdict.None);

        var testCases = savedSubmission.TestCases.OrderBy(x => x.OrderIndex).ToList();
        testCases.Select(x => x.OrderIndex).Should().Equal(1, 2);
        testCases.Select(x => x.ExpectedOutput).Should().Equal("first", "second");
    }

    private async Task<CodeSubmission?> WaitForSubmissionAsync(
        Guid submissionId,
        int attempts = 40,
        int delayMs = 100)
    {
        for (var i = 0; i < attempts; i++)
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CodeExecutionAppDbContext>();

            var submission = await db.CodeSubmissions
                .Include(x => x.TestCases)
                .SingleOrDefaultAsync(x => x.Id == submissionId);

            if (submission is not null)
                return submission;

            await Task.Delay(delayMs);
        }

        return null;
    }
}
