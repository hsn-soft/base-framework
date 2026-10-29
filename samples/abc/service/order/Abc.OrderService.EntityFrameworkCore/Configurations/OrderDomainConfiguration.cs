using Abc.OrderService.Domain.OrderDomain.Consts;
using Abc.OrderService.Domain.OrderDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Abc.OrderService.EntityFrameworkCore.Configurations;

public static class OrderDomainConfiguration
{
    public static void ConfigureOrderDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable(EfCoreOrderDbProperties.DbTablePrefix + OrderConsts.TableName, EfCoreOrderDbProperties.DbSchema);
            order.HasKey(entity => entity.Id);
            order.Property(entity => entity.ShippingAddress).HasMaxLength(OrderConsts.ShippingAddressMaxLength).IsRequired();
            order.Property(entity => entity.Id).ValueGeneratedNever();
            order.Property(entity => entity.CustomerUserName).HasMaxLength(OrderConsts.CustomerUserNameMaxLength).IsRequired();
            order.Property(entity => entity.NormalizedCustomerUserName).HasMaxLength(OrderConsts.CustomerUserNameMaxLength).IsRequired();
            order.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            order.Property(entity => entity.Note).HasMaxLength(OrderConsts.NoteMaxLength);
            order.Property(entity => entity.TotalAmount).HasPrecision(18, 2);
            order.HasIndex(entity => entity.NormalizedCustomerUserName);
            order.HasIndex(entity => entity.CreatedAt);
            order.HasIndex(entity => entity.TenantId);

            // the lines belong to the order: they are read, saved and deleted with it
            order.OwnsMany(entity => entity.Lines, line =>
            {
                line.ToTable(EfCoreOrderDbProperties.DbTablePrefix + OrderConsts.LineTableName, EfCoreOrderDbProperties.DbSchema);
                line.WithOwner().HasForeignKey("OrderId");
                line.Property<int>("Id").ValueGeneratedOnAdd();
                line.HasKey("Id");
                line.Property(entity => entity.ProductName).HasMaxLength(OrderConsts.ProductNameMaxLength).IsRequired();
                line.Property(entity => entity.UnitPrice).HasPrecision(18, 2);
                line.Ignore(entity => entity.Total);
            });
            order.Navigation(entity => entity.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
