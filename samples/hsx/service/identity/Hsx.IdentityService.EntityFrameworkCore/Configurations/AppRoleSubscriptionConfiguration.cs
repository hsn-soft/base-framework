using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Consts;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AppRoleSubscriptionConfiguration
{
    public static void ConfigureAppRoleSubscription(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppRoleSubscription>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AppRoleSubscriptionConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.RoleId, e.SubscriptionId }).IsUnique();
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasOne(e => e.Role).WithMany(role => role.Subscriptions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Subscription).WithMany().HasForeignKey(e => e.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
