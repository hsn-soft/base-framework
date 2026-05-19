using Hhs.AdministrationService.Domain.PermissionDomain.Consts;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using HsnSoft.Base;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Configurations;

public static class PermissionGrantConfiguration
{
    public static void ConfigurePermissionGrantEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<PermissionGrant>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + PermissionGrantConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.Name).HasColumnName(nameof(PermissionGrant.Name)).IsRequired().HasMaxLength(PermissionGrantConsts.NameMaxLength);
            b.Property(x => x.ProviderName).HasColumnName(nameof(PermissionGrant.ProviderName)).IsRequired().HasMaxLength(PermissionGrantConsts.ProviderNameMaxLength);
            b.Property(x => x.ProviderKey).HasColumnName(nameof(PermissionGrant.ProviderKey)).IsRequired().HasMaxLength(PermissionGrantConsts.ProviderKeyMaxLength);

            b.HasIndex(x => new { x.Name, x.ProviderName, x.ProviderKey }).IsUnique();
        });
    }
}