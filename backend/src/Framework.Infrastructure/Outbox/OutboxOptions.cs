namespace Framework.Infrastructure.Outbox;

public class OutboxOptions
{
    public int BatchSize { get; set; } = 20;
    public int PollingIntervalMs { get; set; } = 1000;
}