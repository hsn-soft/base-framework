using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class TenantConfiguration
{
    public static void ConfigureTenant(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + TenantConsts.TableName, EfCoreDbProperties.DbSchema, table =>
            {
                table.HasCheckConstraint("CK_Tenant_NormalizedName_NotEmpty", "\"NormalizedName\" <> ''");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TenantType).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(TenantConsts.TitleMaxLength).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedAccessPath).HasMaxLength(TenantConsts.NormalizedAccessPathMaxLength).IsRequired();
            entity.HasIndex(e => e.NormalizedName).IsUnique();
            entity.HasIndex(e => e.NormalizedAccessPath);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
