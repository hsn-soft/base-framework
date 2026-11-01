using Hsx.ContentService.Domain.CustomerVpSettingDomain.Consts;
using Hsx.ContentService.Domain.CustomerVpSettingDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Configurations;

public static class CustomerVpSettingDomainConfiguration
{
    public static void ConfigureCustomerVpSettingDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<CustomerVpSetting>(entity =>
        {
            entity.ToTable(EfCoreContentDbProperties.DbTablePrefix + CustomerVpSettingConsts.TableName, EfCoreContentDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ScopeKey).HasMaxLength(CustomerVpSettingConsts.ScopeKeyMaxLength).IsRequired();
            entity.Property(e => e.DomainName).HasMaxLength(CustomerVpSettingConsts.DomainNameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedDomainName).HasMaxLength(CustomerVpSettingConsts.DomainNameMaxLength).IsRequired();
            entity.HasIndex(e => e.IsBlocked);
            entity.HasIndex(e => e.ScopeKey);
        });
    }
}
