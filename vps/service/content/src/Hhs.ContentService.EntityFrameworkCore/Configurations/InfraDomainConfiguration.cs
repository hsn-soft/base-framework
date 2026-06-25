using Hhs.ContentService.Domain.InfraDomain.Consts;
using Hhs.ContentService.Domain.InfraDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class InfraDomainConfiguration
{
    public static void ConfigureEventInboxMessageEntity(this ModelBuilder builder)
    {
        builder.Entity<EventInboxMessage>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + EventInboxMessageConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.EventName).HasMaxLength(EventInboxMessageConsts.EventNameMaxLength).IsRequired();
            b.Property(x => x.Status).HasMaxLength(EventInboxMessageConsts.StatusMaxLength).IsRequired();
            b.Property(x => x.Payload).HasColumnType("jsonb");
            b.Property(x => x.CorrelationId).HasMaxLength(EventInboxMessageConsts.CorrelationIdMaxLength);
            b.Property(x => x.ErrorMessage).HasMaxLength(EventInboxMessageConsts.ErrorMessageMaxLength);

            b.HasIndex(x => x.Status);
        });
    }
}