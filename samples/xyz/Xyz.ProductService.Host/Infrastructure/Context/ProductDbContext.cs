using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.ProductService.Domain.ProductDomain.Entities;
using Xyz.ProductService.Infrastructure.Configurations;

namespace Xyz.ProductService.Infrastructure.Context;

/// <summary>The products of the product service, in PostgreSQL.</summary>
public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureProductDomain();
}
