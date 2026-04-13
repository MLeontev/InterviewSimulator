using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Framework.Infrastructure.Outbox;

public class OutboxMessageConfiguration(string schema) : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", schema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => x.ProcessedAt);
    }
}