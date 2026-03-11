using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using CodeExecution.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Verdict = CodeExecution.Domain.Entities.Verdict;

namespace CodeExecution.Infrastructure.Workers;

internal class CodeSubmissionEventPublisher(
    IServiceProvider serviceProvider,
    IBus bus,
    ICodeSubmissionCompletedEventAdapter eventAdapter) : BackgroundService
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
                var @event = eventAdapter.Adapt(submission);
                await bus.Publish(@event, stoppingToken);
                submission.IsEventPublished = true;
            }
            
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}