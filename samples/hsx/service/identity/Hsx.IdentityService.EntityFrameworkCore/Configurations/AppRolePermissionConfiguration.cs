using Hsx.IdentityService.Domain.PermissionDomain.Consts;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AppRolePermissionConfiguration
{
    public static void ConfigureAppRolePermission(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppRolePermission>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AppRolePermissionConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Role).WithMany(role => role.Permissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
