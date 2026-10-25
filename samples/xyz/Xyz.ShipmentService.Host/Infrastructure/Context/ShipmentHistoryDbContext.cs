using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Entities;
using Xyz.ShipmentService.Infrastructure.Configurations;

namespace Xyz.ShipmentService.Infrastructure.Context;

/// <summary>The relational context of a service whose shipments are in MongoDB: how the deliveries ended, on PostgreSQL.</summary>
public sealed class ShipmentHistoryDbContext(DbContextOptions<ShipmentHistoryDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<ShipmentHistory> ShipmentHistories => Set<ShipmentHistory>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureShipmentHistoryDomain();
}
