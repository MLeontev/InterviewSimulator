using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Interview.Infrastructure.Workers;

internal class AiSessionEvaluationWorker(
    IServiceProvider serviceProvider,
    ILogger<AiSessionEvaluationWorker> logger) : BackgroundService
{
    private const int DelayMs = 5000;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessSessionAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(DelayMs, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в AiSessionEvaluationWorker при обработке очереди AI-оценки");
                await Task.Delay(DelayMs, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessSessionAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        
        var sessions = await dbContext.InterviewSessions
            .FromSqlRaw("""
                        UPDATE "Interview"."InterviewSessions" s
                        SET "Status" = 'EvaluatingAi'
                        WHERE s."Id" = (
                            SELECT s2."Id"
                            FROM "Interview"."InterviewSessions" s2
                            WHERE s2."Status" = 'Finished'
                              AND NOT EXISTS (
                                  SELECT 1
                                  FROM "Interview"."InterviewQuestions" q
                                  WHERE q."InterviewSessionId" = s2."Id"
                                    AND q."Status" IN ('Submitted', 'EvaluatingCode', 'EvaluatedCode', 'EvaluatingAi')
                              )
                            ORDER BY s2."FinishedAt" NULLS FIRST, s2."StartedAt"
                            FOR UPDATE SKIP LOCKED
                            LIMIT 1
                        )
                        RETURNING *
                        """)
            .AsNoTracking()
            .ToListAsync(ct);
        
        var session = sessions.FirstOrDefault();

        if (session is null)
            return false;
        
        var result = await sender.Send(new EvaluateInterviewSessionCommand(session.Id), ct);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "AI-оценка сессии {SessionId} завершилась ошибкой: {Code} - {Description}",
                session.Id,
                result.Error.Code,
                result.Error.Description);
        }
        
        return true;
    }
}