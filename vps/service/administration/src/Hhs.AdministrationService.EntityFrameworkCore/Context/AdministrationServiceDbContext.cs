using Hhs.AdministrationService.Domain.InfraDomain.Entities;
using Hhs.AdministrationService.Domain.MenuDomain.Entities;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.EntityFrameworkCore.Configurations;
using HsnSoft.Base;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Context;

public sealed class AdministrationServiceDbContext(
    IServiceProvider provider,
    DbContextOptions<AdministrationServiceDbContext> options
) : BaseEfCoreDbContext<AdministrationServiceDbContext>(options, provider)
{
    public DbSet<EventInboxMessage> EventInboxMessages => Set<EventInboxMessage>();
    public DbSet<AppMenu> AppMenus => Set<AppMenu>();
    public DbSet<AppMenuPermission> AppMenuPermissions => Set<AppMenuPermission>();
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionDependency> PermissionDependencies => Set<PermissionDependency>();
    public DbSet<AppRolePermission> AppRolePermissions => Set<AppRolePermission>();
    public DbSet<AppRolePermissionConstraint> AppRolePermissionConstraints => Set<AppRolePermissionConstraint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureEventInboxMessageEntity();

        modelBuilder.ConfigureAppMenuEntity();
        modelBuilder.ConfigureAppMenuPermissionEntity();

        modelBuilder.ConfigurePermissionGrantEntity();
        modelBuilder.ConfigurePermissionEntity();
        modelBuilder.ConfigurePermissionDependencyEntity();
        modelBuilder.ConfigureAppRolePermissionEntity();
        modelBuilder.ConfigureAppRolePermissionConstraintEntity();
    }
}