using System.Text.Json;
using MassTransit;

namespace Framework.Infrastructure.Outbox;

public class MassTransitEventPublisher(IBus bus) : IEventPublisher
{
    public async Task PublishAsync(string type, string payload, CancellationToken ct)
    {
        var eventType = Type.GetType(type) 
                        ?? throw new InvalidOperationException($"Cannot resolve type: {type}");

        var @event = JsonSerializer.Deserialize(payload, eventType) 
                     ?? throw new InvalidOperationException($"Cannot deserialize payload for type: {type}");

        await bus.Publish(@event, ct);
    }
}