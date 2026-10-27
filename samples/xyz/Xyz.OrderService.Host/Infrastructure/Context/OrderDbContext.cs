using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.OrderService.Domain.OrderDomain.Entities;
using Xyz.OrderService.Infrastructure.Configurations;

namespace Xyz.OrderService.Infrastructure.Context;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : MultiTenantDbContext(options, dataFilter, currentTenant)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureOrderDomain();
}
