using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Outbox;

public class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<OutboxOptions> optionsMonitor,
    ILogger<OutboxProcessor<TContext>> logger,
    string optionsName) : BackgroundService where TContext : ModuleDbContext
{
    private readonly OutboxOptions _options = optionsMonitor.Get(optionsName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[{Context}] Outbox processor error", typeof(TContext).FullName);
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds), 
                stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        
        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(stoppingToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.Payload, stoppingToken);
                
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
                
                logger.LogInformation(
                    "[{Context}] Outbox message {Id} ({Type}) published successfully",
                    typeof(TContext).FullName, message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                
                logger.LogError(ex,
                    "[{Context}] Outbox message {Id} ({Type}) failed",
                    typeof(TContext).FullName, message.Id, message.Type);
            }
        }
        
        await dbContext.SaveChangesAsync(stoppingToken);
    }
}