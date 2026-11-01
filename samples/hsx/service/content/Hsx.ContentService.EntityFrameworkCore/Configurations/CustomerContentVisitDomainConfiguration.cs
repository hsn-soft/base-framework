using Hsx.ContentService.Domain.CustomerContentVisitDomain.Consts;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Configurations;

public static class CustomerContentVisitDomainConfiguration
{
    public static void ConfigureCustomerContentVisitDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<CustomerContentVisit>(entity =>
        {
            entity.ToTable(EfCoreContentDbProperties.DbTablePrefix + CustomerContentVisitConsts.TableName, EfCoreContentDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ScopeKey).HasMaxLength(CustomerContentVisitConsts.ScopeKeyMaxLength).IsRequired();
            entity.Property(e => e.VisitResponse).HasMaxLength(CustomerContentVisitConsts.VisitResponseMaxLength).IsRequired();
            entity.HasIndex(e => e.CustomerContentId);
            entity.HasIndex(e => e.ScopeKey);
        });
    }
}
