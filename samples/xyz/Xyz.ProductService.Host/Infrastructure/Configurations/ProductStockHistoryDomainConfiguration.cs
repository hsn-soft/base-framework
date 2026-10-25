using Microsoft.EntityFrameworkCore;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Consts;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Entities;

namespace Xyz.ProductService.Infrastructure.Configurations;

public static class ProductStockHistoryDomainConfiguration
{
    public static void ConfigureProductStockHistoryDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<ProductStockHistory>(history =>
        {
            history.ToTable(EfCoreStockDbProperties.DbTablePrefix + ProductStockHistoryConsts.TableName, EfCoreStockDbProperties.DbSchema);
            history.HasKey(entity => entity.Id);
            history.Property(entity => entity.Id).ValueGeneratedNever();
            history.Property(entity => entity.ProductName).HasMaxLength(ProductStockHistoryConsts.ProductNameMaxLength).IsRequired();
            history.Property(entity => entity.NormalizedProductName).HasMaxLength(ProductStockHistoryConsts.ProductNameMaxLength).IsRequired();
            history.Property(entity => entity.Reason).HasMaxLength(ProductStockHistoryConsts.ReasonMaxLength).IsRequired();
            history.Property(entity => entity.Note).HasMaxLength(ProductStockHistoryConsts.NoteMaxLength);
            history.Property(entity => entity.Reference).HasMaxLength(ProductStockHistoryConsts.ReferenceMaxLength);
            history.HasIndex(entity => entity.ProductId);
            history.HasIndex(entity => entity.Reference);
            history.HasIndex(entity => entity.TenantId);
        });
    }
}
