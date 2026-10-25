using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.ShipmentService.Domain.CustomerDomain.Entities;
using Xyz.ShipmentService.Domain.PaymentDomain.Entities;
using Xyz.ShipmentService.Domain.ShipmentDomain.Entities;
using Xyz.ShipmentService.Infrastructure.Configurations;

namespace Xyz.ShipmentService.Infrastructure.Context;

/// <summary>The shipments, the customers and the payments of the shipment service, in PostgreSQL.</summary>
public sealed class ShipmentDbContext(DbContextOptions<ShipmentDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureShipmentDomain();
}
