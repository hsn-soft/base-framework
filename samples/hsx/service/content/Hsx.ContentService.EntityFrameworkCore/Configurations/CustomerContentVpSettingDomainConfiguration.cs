using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Consts;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Configurations;

public static class CustomerContentVpSettingDomainConfiguration
{
    public static void ConfigureCustomerContentVpSettingDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<CustomerContentVpSetting>(entity =>
        {
            entity.ToTable(EfCoreContentDbProperties.DbTablePrefix + CustomerContentVpSettingConsts.TableName, EfCoreContentDbProperties.DbSchema, table =>
            {
                table.HasCheckConstraint("CK_CustomerContentVpSetting_NormalizedDomainName_NotEmpty", "\"NormalizedDomainName\" <> ''");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ScopeKey).HasMaxLength(CustomerContentVpSettingConsts.ScopeKeyMaxLength).IsRequired();
            entity.Property(e => e.DomainName).HasMaxLength(CustomerContentVpSettingConsts.DomainNameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedDomainName).HasMaxLength(CustomerContentVpSettingConsts.DomainNameMaxLength).IsRequired();
            entity.HasIndex(e => e.IsBlocked);
            entity.HasIndex(e => e.ScopeKey);
        });
    }
}
