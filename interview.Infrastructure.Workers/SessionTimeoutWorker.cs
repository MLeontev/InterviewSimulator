using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace interview.Infrastructure.Workers;

internal class SessionTimeoutWorker(
    IServiceProvider serviceProvider, 
    ILogger<SessionTimeoutWorker> logger) : BackgroundService
{
    private const int DelayMs = 5000;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessTimedOutSessionAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(DelayMs, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в SessionTimeoutWorker при завершении сессий по таймеру");
                await Task.Delay(DelayMs, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessTimedOutSessionAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        
        var session = await dbContext.InterviewSessions
            .Where(s => s.Status == InterviewStatus.InProgress && s.PlannedEndAt <= DateTime.UtcNow)
            .OrderBy(s => s.PlannedEndAt)
            .FirstOrDefaultAsync(ct);
        
        if (session is null)
            return false;

        session.Status = InterviewStatus.Finished;
        session.FinishedAt = DateTime.UtcNow;
        
        var inProgressQuestions = await dbContext.InterviewQuestions
            .Where(q => q.InterviewSessionId == session.Id && q.Status == QuestionStatus.InProgress)
            .ToListAsync(ct);

        foreach (var q in inProgressQuestions)
            q.Status = QuestionStatus.Skipped;
        
        await dbContext.SaveChangesAsync(ct);
        return true;
    }
}