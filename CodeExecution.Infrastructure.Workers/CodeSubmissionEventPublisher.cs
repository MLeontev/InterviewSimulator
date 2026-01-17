using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using CodeExecution.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Verdict = CodeExecution.Domain.Entities.Verdict;

namespace CodeExecution.Infrastructure.Workers;

internal class CodeSubmissionEventPublisher(IServiceProvider serviceProvider, IBus bus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
            
            var submissions = await dbContext.CodeSubmissions
                .Where(s => !s.IsEventPublished 
                            && (s.Status == ExecutionStatus.Completed || s.Status == ExecutionStatus.Failed))
                .Include(s => s.TestCases.OrderBy(tc => tc.Order))
                .OrderBy(s => s.CompletedAt)
                .ToListAsync(stoppingToken);

            if (submissions.Count == 0)
                await Task.Delay(500, stoppingToken);
            
            foreach (var submission in submissions)
            {
                var @event = ToIntegrationEvent(submission);
                await bus.Publish(@event, stoppingToken);
                submission.IsEventPublished = true;
            }
            
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
    
    private static CodeSubmissionCompleted ToIntegrationEvent(CodeSubmission submission)
    {
        var testCaseResults = new List<TestCaseResultDto>();
        var passedCount = 0;
        
        foreach (var testCase in submission.TestCases)
        {
            testCaseResults.Add(new TestCaseResultDto(
                Input: testCase.Input,
                ExpectedOutput: testCase.ExpectedOutput,
                ActualOutput: testCase.ActualOutput,
                Error: testCase.Error,
                ExitCode: testCase.ExitCode,
                TimeElapsed: testCase.TimeElapsed,
                MemoryUsage: testCase.MemoryUsage,
                Verdict: MapVerdict(testCase.Verdict)));

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
            TestCaseResults: testCaseResults.ToArray(),
            OverallVerdict: overallVerdict,
            PassedCount: passedCount,
            TotalTests: submission.TestCases.Count);
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