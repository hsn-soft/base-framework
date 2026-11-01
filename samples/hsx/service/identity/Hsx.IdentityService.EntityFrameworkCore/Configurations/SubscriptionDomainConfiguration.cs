using Hsx.IdentityService.Domain.SubscriptionDomain.Consts;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class SubscriptionDomainConfiguration
{
    public static void ConfigureSubscriptionDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + SubscriptionConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ProductType).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.ScopeKey).HasMaxLength(SubscriptionConsts.ScopeKeyMaxLength).IsRequired();
            entity.Property(e => e.SettingsJson).HasMaxLength(SubscriptionConsts.SettingsJsonMaxLength);
            entity.HasIndex(e => e.ResellerTenantId);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ProductType);
        });
    }
}
