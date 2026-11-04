using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.MenuDomain.Consts;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Consts;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class PermissionDomainConfiguration
{
    public static void ConfigurePermissionDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Permission>(permission =>
        {
            permission.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + PermissionConsts.PermissionTable, EfCoreIdentityDbProperties.DbSchema);
            permission.HasKey(entity => entity.Id);
            permission.Property(entity => entity.Id).ValueGeneratedNever();
            permission.Property(entity => entity.Code).HasMaxLength(PermissionConsts.CodeMaxLength).IsRequired();
            permission.Property(entity => entity.Type).IsRequired();
            permission.Property(entity => entity.Name).HasMaxLength(PermissionConsts.NameMaxLength).IsRequired();
            permission.Property(entity => entity.Description).HasMaxLength(PermissionConsts.DescriptionMaxLength);
            permission.HasIndex(entity => entity.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(grant =>
        {
            grant.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + PermissionConsts.RolePermissionTable, EfCoreIdentityDbProperties.DbSchema);
            grant.HasKey(entity => new { entity.RoleId, entity.PermissionId });
            grant.HasIndex(entity => entity.TenantId);
            grant.HasOne<AppRole>().WithMany().HasForeignKey(entity => entity.RoleId).OnDelete(DeleteBehavior.Cascade);
            grant.HasOne<Permission>().WithMany().HasForeignKey(entity => entity.PermissionId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}

public static class MenuDomainConfiguration
{
    public static void ConfigureMenuDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Menu>(menu =>
        {
            menu.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + MenuConsts.MenuTable, EfCoreIdentityDbProperties.DbSchema);
            menu.HasKey(entity => entity.Id);
            menu.Property(entity => entity.Id).ValueGeneratedNever();
            menu.Property(entity => entity.Code).HasMaxLength(MenuConsts.CodeMaxLength).IsRequired();
            menu.Property(entity => entity.Title).HasMaxLength(MenuConsts.TitleMaxLength).IsRequired();
            menu.Property(entity => entity.Route).HasMaxLength(MenuConsts.RouteMaxLength);
            menu.Property(entity => entity.Icon).HasMaxLength(MenuConsts.IconMaxLength);
            menu.Property(entity => entity.ChannelType).IsRequired();
            menu.Property(entity => entity.DockType).IsRequired();
            menu.Property(entity => entity.NodeType).IsRequired();
            menu.HasIndex(entity => entity.Code).IsUnique();
            menu.HasOne<Menu>().WithMany().HasForeignKey(entity => entity.ParentId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MenuPermissionMap>(map =>
        {
            map.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + MenuConsts.MenuPermissionMapTable, EfCoreIdentityDbProperties.DbSchema);
            map.HasKey(entity => entity.Id);
            map.Property(entity => entity.Id).ValueGeneratedNever();
            map.Property(entity => entity.RelationType).IsRequired();
            map.HasIndex(entity => new { entity.MenuId, entity.PermissionId }).IsUnique();
            map.HasOne<Menu>().WithMany().HasForeignKey(entity => entity.MenuId).OnDelete(DeleteBehavior.Cascade);
            map.HasOne<Permission>().WithMany().HasForeignKey(entity => entity.PermissionId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
