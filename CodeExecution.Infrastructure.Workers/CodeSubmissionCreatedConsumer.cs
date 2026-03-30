using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using Interview.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CodeExecution.Infrastructure.Workers;

public sealed class CodeSubmissionCreatedConsumer(IDbContext dbContext) : IConsumer<CodeSubmissionCreated>
{
    public async Task Consume(ConsumeContext<CodeSubmissionCreated> context)
    {
        var message = context.Message;

        var submission = await dbContext.CodeSubmissions
            .Include(s => s.TestCases)
            .FirstOrDefaultAsync(s => s.Id == message.SubmissionId, context.CancellationToken);

        if (submission == null)
        {
            submission = new CodeSubmission
            {
                Id = message.SubmissionId,
                CreatedAt = DateTime.UtcNow
            };
            await dbContext.CodeSubmissions.AddAsync(submission, context.CancellationToken);
        }
        else
        {
            dbContext.CodeSubmissionTestCases.RemoveRange(submission.TestCases);
            submission.TestCases.Clear();
        }

        submission.Code = message.Code;
        submission.LanguageCode = message.Language;
        submission.TimeLimitMs = message.MaxTimeSeconds.HasValue
            ? Math.Max(1, message.MaxTimeSeconds.Value) * 1000
            : 1000;
        submission.MemoryLimitMb = message.MaxMemoryMb ?? 64;
        submission.Status = ExecutionStatus.Pending;
        submission.ErrorMessage = string.Empty;
        submission.OverallVerdict = Verdict.None;
        submission.StartedAt = null;
        submission.CompletedAt = null;
        submission.IsEventPublished = false;
        submission.CreatedAt = DateTime.UtcNow;

        submission.TestCases = message.TestCases
            .OrderBy(tc => tc.Order)
            .Select(tc => new CodeSubmissionTestCase
            {
                Id = tc.TestCaseId,
                SubmissionId = submission.Id,
                OrderIndex = tc.Order,
                Input = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                ActualOutput = string.Empty,
                Error = string.Empty,
                ExitCode = 0,
                TimeElapsedMs = 0,
                MemoryUsedMb = 0,
                Verdict = Verdict.None
            })
            .ToList();

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
