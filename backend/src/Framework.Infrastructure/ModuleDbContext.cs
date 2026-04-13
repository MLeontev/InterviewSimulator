using System.Text.Json;
using Framework.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Framework.Infrastructure;

public abstract class ModuleDbContext : DbContext
{
    protected abstract string Schema { get; }
    
    protected ModuleDbContext(DbContextOptions options) : base(options) { }
    
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
    }

    public void AddOutboxMessage(object @event)
    {
        OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = @event.GetType().AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            CreatedAt = DateTime.UtcNow
        });
    }
}