using Hsx.IdentityService.Domain.MenuDomain.Consts;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AppMenuConfiguration
{
    public static void ConfigureAppMenu(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppMenu>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AppMenuConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(AppMenuConsts.CodeMaxLength).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(AppMenuConsts.TitleMaxLength).IsRequired();
            entity.Property(e => e.Route).HasMaxLength(AppMenuConsts.RouteMaxLength);
            entity.Property(e => e.Icon).HasMaxLength(AppMenuConsts.IconMaxLength);
            entity.Property(e => e.ChannelType).IsRequired();
            entity.Property(e => e.DockType).IsRequired();
            entity.Property(e => e.NodeType).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne<AppMenu>().WithMany().HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
