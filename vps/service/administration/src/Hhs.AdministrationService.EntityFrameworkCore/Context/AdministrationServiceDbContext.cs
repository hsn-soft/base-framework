using Hhs.AdministrationService.Domain.MenuDomain.Entities;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.EntityFrameworkCore.Configurations;
using Hhs.Shared.Helper.EventInbox;
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
    public DbSet<OldMenuMap> OldMenuMaps => Set<OldMenuMap>();
    public DbSet<OldMenuPermissionMap> OldMenuPermissionMaps => Set<OldMenuPermissionMap>();

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

        modelBuilder.ConfigureOldMenuMapEntity();
        modelBuilder.ConfigureOldMenuPermissionMapEntity();
    }
}