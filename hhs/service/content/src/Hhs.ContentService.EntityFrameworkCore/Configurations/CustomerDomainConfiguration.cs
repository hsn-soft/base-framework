using Hhs.ContentService.Domain.CustomerDomain.Consts;
using Hhs.ContentService.Domain.CustomerDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class CustomerDomainConfiguration
{
    public static void ConfigureCustomerContentSettingEntity(this ModelBuilder builder)
    {
        builder.Entity<CustomerContentSetting>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerContentSettingConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.DomainName).IsRequired().HasMaxLength(CustomerContentSettingConsts.DomainNameMaxLength);
            b.Property(x => x.IsBlocked).IsRequired();

            b.Property(x => x.DailyDirectVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyDirectVideoGenerationLimit).IsRequired();

            b.Property(x => x.DailyTrendVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyTrendVideoGenerationLimit).IsRequired();
            b.Property(x => x.DailyTrendVideoWaitStatisticHour).IsRequired();
            b.Property(x => x.DailyTrendVideoMinVisitCount).IsRequired();

            b.Property(x => x.DailyAnalysisVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyAnalysisVideoGenerationLimit).IsRequired();

            b.HasMany(x => x.PathFilters)
                .WithOne()
                .HasForeignKey(x => x.CustomerContentSettingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasIndex(x => new { x.TenantId, x.IsDeleted });

            b.HasIndex(x => x.CustomerId).IsUnique();
            b.HasIndex(x => x.DomainName).IsUnique();
        });
    }

    public static void ConfigureCustomerContentSettingPathFilterEntity(this ModelBuilder builder)
    {
        builder.Entity<CustomerContentSettingPathFilter>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerContentSettingPathFilterConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.CustomerContentSettingId).IsRequired();
            b.Property(x => x.PathFilterName).IsRequired().HasMaxLength(CustomerContentSettingPathFilterConsts.PathFilterNameMaxLength);
        });
    }

    public static void ConfigureCustomerVideoGenerationHistoryEntity(this ModelBuilder builder)
    {
        builder.Entity<CustomerVideoGenerationHistory>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerVideoGenerationHistoryConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.VideoGenerationDate).IsRequired();
            b.Property(x => x.VideoGenerationType).IsRequired();
            b.Property(x => x.ContentReferenceIds).IsRequired().HasMaxLength(CustomerVideoGenerationHistoryConsts.ContentReferenceIdsNameMaxLength);

            b.HasIndex(x => new { x.CustomerId, x.VideoGenerationType });
        });
    }
}