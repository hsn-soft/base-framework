using Hhs.AdministrationService.Domain.MenuDomain.Consts;
using Hhs.AdministrationService.Domain.MenuDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Configurations;

public static class MenuDomainConfiguration
{
    public static void ConfigureAppMenuEntity(this ModelBuilder builder) =>
        builder.Entity<AppMenu>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppMenuConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ParentId);

            b.Property(x => x.UniqueCode).HasMaxLength(AppMenuConsts.UniqueCodeMaxLength).IsRequired();

            b.Property(x => x.ChannelType).IsRequired();
            b.Property(x => x.DockType).IsRequired();
            b.Property(x => x.NodeType).IsRequired();

            b.Property(x => x.Title).HasMaxLength(AppMenuConsts.TitleMaxLength);
            b.Property(x => x.Route).HasMaxLength(AppMenuConsts.RouteMaxLength);
            b.Property(x => x.Icon).HasMaxLength(AppMenuConsts.IconMaxLength);

            b.Property(x => x.SortOrder).IsRequired();

            b.HasOne(x => x.Parent)
                .WithMany(x => x.Nodes)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            b.HasIndex(x => x.UniqueCode)
                .IsUnique();
        });

    public static void ConfigureAppMenuPermissionEntity(this ModelBuilder builder) =>
        builder.Entity<AppMenuPermission>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppMenuPermissionConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.RelationType).IsRequired();
            b.Property(x => x.SortOrder).IsRequired();

            b.HasOne(x => x.AppMenu)
                .WithMany(x => x.MenuPermissions)
                .HasForeignKey(x => x.AppMenuId)
                .IsRequired();

            b.HasIndex(x => new { x.AppMenuId, x.PermissionId })
                .IsUnique();
        });
}