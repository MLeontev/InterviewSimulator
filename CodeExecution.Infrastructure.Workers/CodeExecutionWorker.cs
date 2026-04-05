using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using CodeExecution.IntegrationEvents;
using CodeExecution.UseCases.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Verdict = CodeExecution.Domain.Entities.Verdict;

namespace CodeExecution.Infrastructure.Workers;

internal class CodeExecutionWorker(IServiceProvider serviceProvider) : BackgroundService
{
    private const int DelayMs = 5000;
    private const int SubmissionsToPublishCount = 10;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await ProcessPendingSubmissionAsync(stoppingToken);
            var published = await PublishCompletedEventsAsync(stoppingToken);
            
            if (!processed && published == 0)
                await Task.Delay(DelayMs, stoppingToken);
        }
    }

    private async Task<bool> ProcessPendingSubmissionAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        
        var submissionId = dbContext.CodeSubmissions
            .FromSqlRaw("""
                        UPDATE "CodeExecution"."CodeSubmissions"
                        SET "Status" = 'Running'
                        WHERE "Id" = (
                            SELECT "Id"
                            FROM "CodeExecution"."CodeSubmissions"
                            WHERE "Status" = 'Pending'
                            ORDER BY "CreatedAt"
                            FOR UPDATE SKIP LOCKED
                            LIMIT 1
                        )
                        RETURNING *
                        """)
            .AsNoTracking()
            .AsEnumerable()
            .Select(s => s.Id)
            .FirstOrDefault();

        if (submissionId == Guid.Empty)
            return false;

        await sender.Send(new CheckSubmissionCommand(submissionId), stoppingToken);
        return true;
    }

    private async Task<int> PublishCompletedEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var submissions = await dbContext.CodeSubmissions
            .Where(x => !x.IsEventPublished &&
                        (x.Status == ExecutionStatus.Completed || x.Status == ExecutionStatus.Failed))
            .Include(x => x.TestCases.OrderBy(tc => tc.OrderIndex))
            .OrderBy(x => x.CompletedAt)
            .Take(SubmissionsToPublishCount)
            .ToListAsync(stoppingToken);
        
        var published = 0;
        
        foreach (var submission in submissions)
        {
            await bus.Publish(ToIntegrationEvent(submission), stoppingToken);
            submission.IsEventPublished = true;
            await dbContext.SaveChangesAsync(stoppingToken);
            published++;
        }
        
        return published;
    }
    
    private static CodeSubmissionCompleted ToIntegrationEvent(CodeSubmission submission)
    {
        var testCaseResults = new List<TestCaseResultDto>(submission.TestCases.Count);
        var passedCount = 0;
        
        foreach (var testCase in submission.TestCases)
        {
            var verdict = MapVerdict(testCase.Verdict);

            testCaseResults.Add(new TestCaseResultDto(
                InterviewTestCaseId: testCase.InterviewTestCaseId,
                OrderIndex: testCase.OrderIndex,
                Input: testCase.Input,
                ExpectedOutput: testCase.ExpectedOutput,
                ActualOutput: testCase.ActualOutput ?? string.Empty,
                Error: testCase.Error ?? string.Empty,
                ExitCode: testCase.ExitCode ?? 0,
                TimeElapsedMs: testCase.TimeElapsedMs ?? 0,
                MemoryUsedMb: testCase.MemoryUsedMb ?? 0,
                Verdict: verdict));

            if (testCase.Verdict == Verdict.OK)
                passedCount++;
            else
                break;
        }
        
        var overallVerdict = submission.Status == ExecutionStatus.Failed
            ? IntegrationEvents.Verdict.FailedSystem
            : MapVerdict(submission.OverallVerdict);

        return new CodeSubmissionCompleted(
            SubmissionId: submission.Id,
            InterviewQuestionId: submission.InterviewQuestionId,
            TestCaseResults: testCaseResults,
            OverallVerdict: overallVerdict,
            PassedCount: passedCount,
            TotalTests: submission.TestCases.Count,
            ErrorMessage: submission.ErrorMessage);
    }
    
    private static IntegrationEvents.Verdict MapVerdict(Verdict verdict) =>
        verdict switch
        {
            Verdict.OK => IntegrationEvents.Verdict.OK,
            Verdict.CE => IntegrationEvents.Verdict.CE,
            Verdict.RE => IntegrationEvents.Verdict.RE,
            Verdict.TLE => IntegrationEvents.Verdict.TLE,
            Verdict.MLE => IntegrationEvents.Verdict.MLE,
            Verdict.WA => IntegrationEvents.Verdict.WA,
            _ => IntegrationEvents.Verdict.FailedSystem
        };
}
