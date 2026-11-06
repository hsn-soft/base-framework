using Hsx.IdentityService.Domain.AppRoleDomain.Consts;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AppRoleConfiguration
{
    public static void ConfigureAppRole(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AppRoleConsts.TableName, EfCoreDbProperties.DbSchema, table =>
            {
                table.HasCheckConstraint("CK_AppRole_NormalizedName_NotEmpty", "\"NormalizedName\" <> ''");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(AppRoleConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(AppRoleConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.RoleType).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.NormalizedName }).IsUnique();
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.CustomerId);
        });
    }
}
