using Hsx.IdentityService.Domain.PermissionDomain.Consts;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class PermissionConfiguration
{
    public static void ConfigurePermission(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + PermissionConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(PermissionConsts.CodeMaxLength).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(PermissionConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(PermissionConsts.DescriptionMaxLength);
            entity.HasIndex(e => e.Code).IsUnique();
        });
    }
}
