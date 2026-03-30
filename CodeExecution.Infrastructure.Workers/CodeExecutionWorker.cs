using CodeExecution.Infrastructure.Interfaces.DataAccess;
using CodeExecution.UseCases.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CodeExecution.Infrastructure.Workers;

internal class CodeExecutionWorker(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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

            if (submissionId != Guid.Empty)
            {
                var command = new CheckSubmissionCommand(submissionId);
                await sender.Send(command, stoppingToken);
            }
            else
            {
                await Task.Delay(500, stoppingToken);
            }
        }
    }
}
