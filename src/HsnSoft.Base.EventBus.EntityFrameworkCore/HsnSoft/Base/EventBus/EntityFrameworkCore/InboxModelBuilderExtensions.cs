using System;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.EventBus.EntityFrameworkCore;

public static class InboxModelBuilderExtensions
{
    /// <summary>Maps the inbox table (<paramref name="tableName"/>, named like the other tables of the service) into the context of the service; call it from the model configuration.</summary>
    public static ModelBuilder ConfigureEventInbox(this ModelBuilder modelBuilder, string tableName = "InboxMessage", string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        modelBuilder.Entity<InboxMessage>(inbox =>
        {
            inbox.ToTable(tableName, schema);
            inbox.HasKey(message => message.Id);
            inbox.Property(message => message.Id).ValueGeneratedNever();
            inbox.Property(message => message.EventName).HasMaxLength(InboxMessage.EventNameMaxLength).IsRequired();
            inbox.Property(message => message.CorrelationId).HasMaxLength(InboxMessage.CorrelationIdMaxLength);
            inbox.Property(message => message.Status).HasMaxLength(InboxMessage.StatusMaxLength).IsRequired();
            inbox.Property(message => message.Error).HasMaxLength(InboxMessage.ErrorMaxLength);

            // what the cleanup looks for: the handled rows that are old
            inbox.HasIndex(message => new { message.Status, message.ProcessedAt });
        });
        return modelBuilder;
    }
}
