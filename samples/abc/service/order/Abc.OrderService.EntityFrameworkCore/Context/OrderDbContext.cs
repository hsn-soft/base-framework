using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Abc.OrderService.EntityFrameworkCore.Context;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureOrderDomain();
}
