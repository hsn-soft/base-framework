using Hsx.IdentityService.Domain.MenuDomain.Consts;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AppMenuPermissionConfiguration
{
    public static void ConfigureAppMenuPermission(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppMenuPermission>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AppMenuPermissionConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RelationType).IsRequired();
            entity.HasIndex(e => new { e.MenuId, e.PermissionId }).IsUnique();
            entity.HasOne<AppMenu>().WithMany(menu => menu.Permissions).HasForeignKey(e => e.MenuId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
