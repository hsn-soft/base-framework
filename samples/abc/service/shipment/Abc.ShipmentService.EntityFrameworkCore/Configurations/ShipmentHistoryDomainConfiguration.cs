using Abc.ShipmentService.Domain.ShipmentHistoryDomain.Consts;
using Abc.ShipmentService.Domain.ShipmentHistoryDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Abc.ShipmentService.EntityFrameworkCore.Configurations;

public static class ShipmentHistoryDomainConfiguration
{
    public static void ConfigureShipmentHistoryDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<ShipmentHistory>(history =>
        {
            history.ToTable(EfCoreShipmentHistoryDbProperties.DbTablePrefix + ShipmentHistoryConsts.TableName, EfCoreShipmentHistoryDbProperties.DbSchema);
            history.HasKey(entity => entity.Id);
            history.Property(entity => entity.Id).ValueGeneratedNever();
            history.Property(entity => entity.TrackingNumber).HasMaxLength(ShipmentHistoryConsts.TrackingNumberMaxLength).IsRequired();
            history.Property(entity => entity.Outcome).HasMaxLength(ShipmentHistoryConsts.OutcomeMaxLength).IsRequired();
            history.Property(entity => entity.Note).HasMaxLength(ShipmentHistoryConsts.NoteMaxLength);
            history.HasIndex(entity => entity.ShipmentId);
            history.HasIndex(entity => entity.OrderId);
        });
    }
}
