using Microsoft.EntityFrameworkCore;
using Xyz.ShipmentService.Domain.CustomerDomain.Consts;
using Xyz.ShipmentService.Domain.CustomerDomain.Entities;
using Xyz.ShipmentService.Domain.PaymentDomain.Consts;
using Xyz.ShipmentService.Domain.PaymentDomain.Entities;
using Xyz.ShipmentService.Domain.ShipmentDomain.Consts;
using Xyz.ShipmentService.Domain.ShipmentDomain.Entities;

namespace Xyz.ShipmentService.Infrastructure.Configurations;

public static class ShipmentDomainConfiguration
{
    public static void ConfigureShipmentDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Shipment>(shipment =>
        {
            shipment.ToTable(EfCoreShipmentDbProperties.DbTablePrefix + ShipmentConsts.TableName, EfCoreShipmentDbProperties.DbSchema);
            shipment.HasKey(entity => entity.Id);
            shipment.Property(entity => entity.Id).ValueGeneratedNever();
            shipment.Property(entity => entity.TrackingNumber).HasMaxLength(ShipmentConsts.TrackingNumberMaxLength).IsRequired();
            shipment.Property(entity => entity.Carrier).HasMaxLength(ShipmentConsts.CarrierMaxLength).IsRequired();
            shipment.Property(entity => entity.NormalizedCarrier).HasMaxLength(ShipmentConsts.CarrierMaxLength).IsRequired();
            shipment.Property(entity => entity.Address).HasMaxLength(ShipmentConsts.AddressMaxLength).IsRequired();
            shipment.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            shipment.HasIndex(entity => entity.TrackingNumber).IsUnique();
            shipment.HasIndex(entity => entity.OrderId);
            shipment.HasIndex(entity => entity.TenantId);
        });

        modelBuilder.Entity<Customer>(customer =>
        {
            customer.ToTable(EfCoreShipmentDbProperties.DbTablePrefix + CustomerConsts.TableName, EfCoreShipmentDbProperties.DbSchema);
            customer.HasKey(entity => entity.Id);
            customer.Property(entity => entity.Id).ValueGeneratedNever();
            customer.Property(entity => entity.UserName).HasMaxLength(CustomerConsts.UserNameMaxLength).IsRequired();
            customer.Property(entity => entity.NormalizedUserName).HasMaxLength(CustomerConsts.UserNameMaxLength).IsRequired();
            customer.Property(entity => entity.FullName).HasMaxLength(CustomerConsts.FullNameMaxLength).IsRequired();
            customer.Property(entity => entity.NormalizedFullName).HasMaxLength(CustomerConsts.FullNameMaxLength).IsRequired();
            customer.Property(entity => entity.Email).HasMaxLength(CustomerConsts.EmailMaxLength);
            customer.Property(entity => entity.Phone).HasMaxLength(CustomerConsts.PhoneMaxLength);
            customer.Property(entity => entity.Address).HasMaxLength(CustomerConsts.AddressMaxLength);
            customer.HasIndex(entity => new { entity.TenantId, entity.NormalizedUserName }).IsUnique();
        });

        modelBuilder.Entity<Payment>(payment =>
        {
            payment.ToTable(EfCoreShipmentDbProperties.DbTablePrefix + PaymentConsts.TableName, EfCoreShipmentDbProperties.DbSchema);
            payment.HasKey(entity => entity.Id);
            payment.Property(entity => entity.Id).ValueGeneratedNever();
            payment.Property(entity => entity.Amount).HasPrecision(18, 2);
            payment.Property(entity => entity.Currency).HasMaxLength(PaymentConsts.CurrencyLength).IsRequired();
            payment.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            payment.Property(entity => entity.Reference).HasMaxLength(PaymentConsts.ReferenceMaxLength);
            payment.Property(entity => entity.FailureReason).HasMaxLength(PaymentConsts.ReasonMaxLength);
            payment.HasIndex(entity => entity.OrderId);
            payment.HasIndex(entity => entity.CustomerId);
            payment.HasIndex(entity => entity.TenantId);
        });
    }
}
