using Edx.IdentityService.Domain.IdentityDomain.Consts;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore.Configurations;

public static class IdentityDomainConfiguration
{
    public static void ConfigureIdentityDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.TenantTable, EfCoreIdentityDbProperties.DbSchema);
            tenant.HasKey(entity => entity.Id);
            tenant.Property(entity => entity.Id).ValueGeneratedNever();
            tenant.Property(entity => entity.Code).HasMaxLength(IdentityConsts.CodeMaxLength).IsRequired();
            tenant.Property(entity => entity.Name).HasMaxLength(IdentityConsts.NameMaxLength).IsRequired();
            tenant.HasIndex(entity => entity.Code).IsUnique();
        });

        modelBuilder.Entity<Role>(role =>
        {
            role.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.RoleTable, EfCoreIdentityDbProperties.DbSchema);
            role.HasKey(entity => entity.Id);
            role.Property(entity => entity.Id).ValueGeneratedNever();
            role.Property(entity => entity.Name).HasMaxLength(IdentityConsts.NameMaxLength).IsRequired();
            role.Property(entity => entity.NormalizedName).HasMaxLength(IdentityConsts.NameMaxLength).IsRequired();

            // a name is unique in a tenant: every tenant has its own admin
            role.HasIndex(entity => new { entity.TenantId, entity.NormalizedName }).IsUnique();
        });

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.UserTable, EfCoreIdentityDbProperties.DbSchema);
            user.HasKey(entity => entity.Id);
            user.Property(entity => entity.Id).ValueGeneratedNever();
            user.Property(entity => entity.UserName).HasMaxLength(IdentityConsts.UserNameMaxLength).IsRequired();
            user.Property(entity => entity.NormalizedUserName).HasMaxLength(IdentityConsts.UserNameMaxLength).IsRequired();
            user.Property(entity => entity.Email).HasMaxLength(IdentityConsts.EmailMaxLength).IsRequired();
            user.Property(entity => entity.NormalizedEmail).HasMaxLength(IdentityConsts.EmailMaxLength).IsRequired();
            user.Property(entity => entity.DisplayName).HasMaxLength(IdentityConsts.NameMaxLength).IsRequired();
            user.Property(entity => entity.PasswordHash).HasMaxLength(IdentityConsts.PasswordHashMaxLength).IsRequired();
            user.HasIndex(entity => new { entity.TenantId, entity.NormalizedUserName }).IsUnique();
        });

        modelBuilder.Entity<UserRole>(userRole =>
        {
            userRole.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.UserRoleTable, EfCoreIdentityDbProperties.DbSchema);
            userRole.HasKey(entity => new { entity.UserId, entity.RoleId });
            userRole.HasIndex(entity => entity.TenantId);
            userRole.HasOne<User>().WithMany().HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Cascade);
            userRole.HasOne<Role>().WithMany().HasForeignKey(entity => entity.RoleId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Permission>(permission =>
        {
            permission.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.PermissionTable, EfCoreIdentityDbProperties.DbSchema);
            permission.HasKey(entity => entity.Id);
            permission.Property(entity => entity.Id).ValueGeneratedNever();
            permission.Property(entity => entity.Code).HasMaxLength(IdentityConsts.PermissionCodeMaxLength).IsRequired();
            permission.Property(entity => entity.Name).HasMaxLength(IdentityConsts.NameMaxLength).IsRequired();
            permission.Property(entity => entity.Description).HasMaxLength(IdentityConsts.PermissionDescriptionMaxLength);

            // the type is kept as its name, so a person reading the table understands it
            permission.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
            permission.HasIndex(entity => entity.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(grant =>
        {
            grant.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.RolePermissionTable, EfCoreIdentityDbProperties.DbSchema);
            grant.HasKey(entity => new { entity.RoleId, entity.PermissionId });
            grant.HasIndex(entity => entity.TenantId);
            grant.HasOne<Role>().WithMany().HasForeignKey(entity => entity.RoleId).OnDelete(DeleteBehavior.Cascade);
            grant.HasOne<Permission>().WithMany().HasForeignKey(entity => entity.PermissionId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RolePermissionConstraint>(grant =>
        {
            grant.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.RolePermissionConstraintTable, EfCoreIdentityDbProperties.DbSchema);
            grant.HasKey(entity => entity.Id);
            grant.Property(entity => entity.Id).ValueGeneratedNever();
            grant.Property(entity => entity.Value).HasMaxLength(IdentityConsts.PermissionValueMaxLength).IsRequired();

            // a role has one value of a constraint
            grant.HasIndex(entity => new { entity.RoleId, entity.PermissionId }).IsUnique();
            grant.HasIndex(entity => entity.TenantId);
            grant.HasOne<Role>().WithMany().HasForeignKey(entity => entity.RoleId).OnDelete(DeleteBehavior.Cascade);
            grant.HasOne<Permission>().WithMany().HasForeignKey(entity => entity.PermissionId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Menu>(menu =>
        {
            menu.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.MenuTable, EfCoreIdentityDbProperties.DbSchema);
            menu.HasKey(entity => entity.Id);
            menu.Property(entity => entity.Id).ValueGeneratedNever();
            menu.Property(entity => entity.Code).HasMaxLength(IdentityConsts.MenuCodeMaxLength).IsRequired();
            menu.Property(entity => entity.Title).HasMaxLength(IdentityConsts.MenuTitleMaxLength).IsRequired();
            menu.Property(entity => entity.Route).HasMaxLength(IdentityConsts.MenuRouteMaxLength);
            menu.Property(entity => entity.Icon).HasMaxLength(IdentityConsts.MenuIconMaxLength);
            menu.Property(entity => entity.ChannelType).HasConversion<string>().HasMaxLength(16).IsRequired();
            menu.Property(entity => entity.DockType).HasConversion<string>().HasMaxLength(16).IsRequired();
            menu.Property(entity => entity.NodeType).HasConversion<string>().HasMaxLength(16).IsRequired();
            menu.HasIndex(entity => entity.Code).IsUnique();
            menu.HasOne<Menu>().WithMany().HasForeignKey(entity => entity.ParentId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MenuPermissionMap>(map =>
        {
            map.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + IdentityConsts.MenuPermissionMapTable, EfCoreIdentityDbProperties.DbSchema);
            map.HasKey(entity => entity.Id);
            map.Property(entity => entity.Id).ValueGeneratedNever();
            map.Property(entity => entity.RelationType).HasConversion<string>().HasMaxLength(16).IsRequired();
            map.HasIndex(entity => new { entity.MenuId, entity.PermissionId }).IsUnique();
            map.HasOne<Menu>().WithMany().HasForeignKey(entity => entity.MenuId).OnDelete(DeleteBehavior.Cascade);
            map.HasOne<Permission>().WithMany().HasForeignKey(entity => entity.PermissionId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
