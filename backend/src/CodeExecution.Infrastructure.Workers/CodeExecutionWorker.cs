using CodeExecution.Infrastructure.Interfaces.DataAccess;
using CodeExecution.UseCases.CodeSubmission.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeExecution.Infrastructure.Workers;

internal class CodeExecutionWorker(
    IServiceProvider serviceProvider,
    ILogger<CodeExecutionWorker> logger) : BackgroundService
{
    private const int DelayMs = 2000;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessPendingSubmissionAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(DelayMs, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "CodeExecutionWorker error");
                await Task.Delay(DelayMs, stoppingToken);
            }
        }
    }

    internal async Task<bool> ProcessPendingSubmissionAsync(CancellationToken stoppingToken)
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
}
