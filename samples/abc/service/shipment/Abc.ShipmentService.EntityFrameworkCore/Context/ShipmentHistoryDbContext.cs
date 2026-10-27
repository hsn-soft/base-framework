using Abc.ShipmentService.Domain.ShipmentHistoryDomain.Entities;
using Abc.ShipmentService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Abc.ShipmentService.EntityFrameworkCore.Context;

/// <summary>The relational context of a service whose shipments are in MongoDB: how the deliveries ended, on PostgreSQL.</summary>
public sealed class ShipmentHistoryDbContext(DbContextOptions<ShipmentHistoryDbContext> options, IDataFilter? dataFilter = null)
    : BaseDbContext(options, dataFilter)
{
    public DbSet<ShipmentHistory> ShipmentHistories => Set<ShipmentHistory>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureShipmentHistoryDomain();
}
