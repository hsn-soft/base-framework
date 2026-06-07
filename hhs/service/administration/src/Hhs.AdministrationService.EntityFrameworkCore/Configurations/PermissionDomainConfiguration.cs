using Hhs.AdministrationService.Domain.PermissionDomain.Consts;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Configurations;

public static class PermissionDomainConfiguration
{
    extension(ModelBuilder builder)
    {
        public void ConfigurePermissionGrantEntity() =>
            builder.Entity<PermissionGrant>(b =>
            {
                b.ToTable(EfCoreDbProperties.DbTablePrefix + PermissionGrantConsts.TableName, EfCoreDbProperties.DbSchema);
                b.HasKey(ci => ci.Id);

                b.Property(x => x.Name).HasColumnName(nameof(PermissionGrant.Name)).IsRequired().HasMaxLength(PermissionGrantConsts.NameMaxLength);
                b.Property(x => x.ProviderName).HasColumnName(nameof(PermissionGrant.ProviderName)).IsRequired().HasMaxLength(PermissionGrantConsts.ProviderNameMaxLength);
                b.Property(x => x.ProviderKey).HasColumnName(nameof(PermissionGrant.ProviderKey)).IsRequired().HasMaxLength(PermissionGrantConsts.ProviderKeyMaxLength);

                b.HasIndex(x => new { x.Name, x.ProviderName, x.ProviderKey })
                    .IsUnique();
            });

        public void ConfigurePermissionEntity() =>
            builder.Entity<Permission>(b =>
            {
                b.ToTable(EfCoreDbProperties.DbTablePrefix + PermissionConsts.TableName, EfCoreDbProperties.DbSchema);
                b.HasKey(x => x.Id);

                b.Property(x => x.UniqueCode).HasMaxLength(PermissionConsts.UniqueCodeMaxLength).IsRequired();

                b.Property(x => x.PermissionType).IsRequired();

                b.Property(x => x.Name).HasMaxLength(PermissionConsts.NameMaxLength);
                b.Property(x => x.Description).HasMaxLength(PermissionConsts.DescriptionMaxLength);

                b.HasIndex(x => x.UniqueCode)
                    .IsUnique();
            });

        public void ConfigurePermissionDependencyEntity() =>
            builder.Entity<PermissionDependency>(b =>
            {
                b.ToTable(EfCoreDbProperties.DbTablePrefix + PermissionDependencyConsts.TableName, EfCoreDbProperties.DbSchema);
                b.HasKey(x => x.Id);

                b.HasIndex(x => new { x.PermissionId, x.DependsOnPermissionId })
                    .IsUnique();

                b.HasOne(x => x.Permission)
                    .WithMany(x => x.ParentPermissions)
                    .HasForeignKey(x => x.PermissionId)
                    .OnDelete(DeleteBehavior.Restrict) // no-cascade delete
                    .IsRequired();

                b.HasOne(x => x.DependsOnPermission)
                    .WithMany(x => x.ChildPermissions)
                    .HasForeignKey(x => x.DependsOnPermissionId)
                    .OnDelete(DeleteBehavior.Restrict) // no-cascade delete
                    .IsRequired();
            });

        public void ConfigureAppRolePermissionEntity() =>
            builder.Entity<AppRolePermission>(b =>
            {
                b.ToTable(EfCoreDbProperties.DbTablePrefix + AppRolePermissionConsts.TableName, EfCoreDbProperties.DbSchema);
                b.HasKey(x => x.Id);

                b.HasIndex(x => new { x.AppRoleId, x.PermissionId })
                    .IsUnique();
            });

        public void ConfigureAppRolePermissionConstraintEntity() =>
            builder.Entity<AppRolePermissionConstraint>(b =>
            {
                b.ToTable(EfCoreDbProperties.DbTablePrefix + AppRolePermissionConstraintConsts.TableName, EfCoreDbProperties.DbSchema);
                b.HasKey(x => x.Id);

                b.Property(x => x.Value).HasMaxLength(AppRolePermissionConstraintConsts.ValueMaxLength).IsRequired();

                b.HasIndex(x => new { x.AppRoleId, x.PermissionId })
                    .IsUnique();
            });
    }
}