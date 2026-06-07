using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Configurations;

public static class TenantDomainConfiguration
{
    public static void ConfigureTenantEntity(this ModelBuilder builder) =>
        builder.Entity<Tenant>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + TenantConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.IsSystemTenant).IsRequired();
            b.Property(x => x.ParentId);
            b.Property(x => x.Title).HasMaxLength(TenantConsts.TitleMaxLength).IsRequired();
            b.Property(x => x.Name).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            b.Property(x => x.NormalizedAccessPath).HasMaxLength(TenantConsts.NormalizedAccessPathMaxLength).IsRequired();

            b.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            b.HasIndex(x => x.NormalizedName)
                .IsUnique();
            b.HasIndex(x => x.ParentId);
            b.HasIndex(x => x.NormalizedAccessPath);
            b.HasIndex(x => x.IsSystemTenant);
        });
}