using Microsoft.EntityFrameworkCore;
using Xyz.ServiceOrder.Host.Domain;

namespace Xyz.ServiceOrder.Host.Persistence;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("orders");
            order.HasKey(x => x.Id);
            order.Property(x => x.CustomerId).HasMaxLength(100).IsRequired();
            order.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            order.Property(x => x.RejectReason).HasMaxLength(500);
            order.Property(x => x.TrackingNumber).HasMaxLength(50);
            order.Property(x => x.TotalPrice).HasPrecision(18, 2);
            order.Property(x => x.Version).IsRowVersion();
            order.HasIndex(x => x.CustomerId);
        });
    }
}
