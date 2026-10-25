using Microsoft.EntityFrameworkCore;
using Xyz.ProductService.Domain.ProductDomain.Consts;
using Xyz.ProductService.Domain.ProductDomain.Entities;

namespace Xyz.ProductService.Infrastructure.Configurations;

public static class ProductDomainConfiguration
{
    public static void ConfigureProductDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable(EfCoreProductDbProperties.DbTablePrefix + ProductConsts.TableName, EfCoreProductDbProperties.DbSchema);
            product.HasKey(entity => entity.Id);
            product.Property(entity => entity.Id).ValueGeneratedNever();
            product.Property(entity => entity.Name).HasMaxLength(ProductConsts.NameMaxLength).IsRequired();
            product.Property(entity => entity.NormalizedName).HasMaxLength(ProductConsts.NameMaxLength).IsRequired();
            product.Property(entity => entity.Sku).HasMaxLength(ProductConsts.SkuMaxLength).IsRequired();
            product.Property(entity => entity.Description).HasMaxLength(ProductConsts.DescriptionMaxLength);
            product.Property(entity => entity.Price).HasPrecision(18, 2);
            product.HasIndex(entity => entity.Sku).IsUnique();
            product.HasIndex(entity => entity.NormalizedName);
            product.HasIndex(entity => entity.TenantId);
        });
    }
}
