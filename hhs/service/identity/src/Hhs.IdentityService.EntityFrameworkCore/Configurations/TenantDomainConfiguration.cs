using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Configurations;

public static class TenantDomainConfiguration
{
    public static void ConfigureTenantEntity(this ModelBuilder builder) =>
        builder.Entity<Tenant>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + TenantConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ParentId);
            b.Property(x => x.TenantType).IsRequired();
            b.Property(x => x.Title).HasMaxLength(TenantConsts.TitleMaxLength).IsRequired();
            b.Property(x => x.Name).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            b.Property(x => x.NormalizedAccessPath).HasMaxLength(TenantConsts.NormalizedAccessPathMaxLength).IsRequired();

            b.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            b.HasIndex(x => x.NormalizedName)
                .IsUnique();
            b.HasIndex(x => x.ParentId);
            b.HasIndex(x => x.NormalizedAccessPath);
            b.HasIndex(x => x.TenantType);
        });

    public static void ConfigureCompanyEntity(this ModelBuilder builder) =>
        builder.Entity<Company>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CompanyConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(CompanyConsts.TitleMaxLength).IsRequired();
            b.Property(x => x.Name).HasMaxLength(CompanyConsts.NameMaxLength).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(CompanyConsts.NameMaxLength).IsRequired();

            b.HasIndex(x => x.NormalizedName)
                .IsUnique();
        });

    public static void ConfigureCustomerEntity(this ModelBuilder builder) =>
        builder.Entity<Customer>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.CompanyId).IsRequired();
            b.Property(x => x.Domain).HasMaxLength(CustomerConsts.DomainMaxLength).IsRequired();
            b.Property(x => x.NormalizedDomain).HasMaxLength(CustomerConsts.DomainMaxLength).IsRequired();

            b.HasOne(x => x.Company)
                .WithMany(x => x.Customers)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.NormalizedDomain)
                .IsUnique();
        });

    public static void ConfigureProductTypeEntity(this ModelBuilder builder) =>
        builder.Entity<ProductType>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ProductTypeConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(ProductTypeConsts.CodeMaxLength).IsRequired();
            b.Property(x => x.NormalizedCode).HasMaxLength(ProductTypeConsts.CodeMaxLength).IsRequired();
            b.Property(x => x.Name).HasMaxLength(ProductTypeConsts.NameMaxLength).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(ProductTypeConsts.NameMaxLength).IsRequired();

            b.HasIndex(x => x.NormalizedCode)
                .IsUnique();

            b.HasIndex(x => x.NormalizedName)
                .IsUnique();
        });

    public static void ConfigureSubscriptionEntity(this ModelBuilder builder) =>
        builder.Entity<Subscription>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + SubscriptionConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ResellerTenantId).IsRequired();
            b.Property(x => x.CompanyId).IsRequired();
            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.ProductTypeId).IsRequired();

            b.Property(x => x.ValidFrom).IsRequired();
            b.Property(x => x.IsBlocked).IsRequired();
            b.Property(x => x.ValidTo);

            b.Property(x => x.SettingsJson).IsRequired().HasColumnType("jsonb");

            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ProductType)
                .WithMany()
                .HasForeignKey(x => x.ProductTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.ResellerTenantId, x.CompanyId, x.CustomerId, x.ProductTypeId })
                .IsUnique();
        });
}