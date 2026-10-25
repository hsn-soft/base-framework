using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Entities;
using Xyz.ProductService.Infrastructure.Configurations;

namespace Xyz.ProductService.Infrastructure.Context;

/// <summary>The second context of the product service, on its own database: the history of the stock of the products.</summary>
public sealed class StockDbContext(DbContextOptions<StockDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<ProductStockHistory> StockHistories => Set<ProductStockHistory>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureProductStockHistoryDomain();
}
