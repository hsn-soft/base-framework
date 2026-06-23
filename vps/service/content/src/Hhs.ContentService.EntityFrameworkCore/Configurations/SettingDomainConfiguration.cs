using System.Text.Json;
using Hhs.ContentService.Domain.SettingDomain.Consts;
using Hhs.ContentService.Domain.SettingDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class SettingDomainConfiguration
{
    public static void ConfigureCustomerVpSettingEntity(this ModelBuilder builder)
    {
        builder.Entity<CustomerVpSetting>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerVpSettingConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(CustomerVpSettingConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.DomainName).HasMaxLength(CustomerVpSettingConsts.DomainNameMaxLength).IsRequired();
            b.Property(x => x.IsBlocked).IsRequired();

            b.Property(x => x.DailyDirectVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyDirectVideoGenerationLimit).IsRequired();

            b.Property(x => x.DailyTrendVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyTrendVideoGenerationLimit).IsRequired();
            b.Property(x => x.DailyTrendVideoWaitStatisticHour).IsRequired();
            b.Property(x => x.DailyTrendVideoMinVisitCount).IsRequired();

            b.Property(x => x.DailyAnalysisVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyAnalysisVideoGenerationLimit).IsRequired();

            b.Property(x => x.IncludePathFilters)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>());

            b.Property(x => x.ExcludePathFilters)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>());

            b.HasIndex(x => x.ScopeKey).IsUnique();
            b.HasIndex(x => x.DomainName).IsUnique();
        });
    }

    public static void ConfigureContentVideoGenerationLimitEntity(this ModelBuilder builder)
    {
        builder.Entity<ContentVideoGenerationLimit>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ContentVideoGenerationLimitConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(ContentVideoGenerationLimitConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.VideoGenerationDate).IsRequired();
            b.Property(x => x.VideoGenerationType).IsRequired();
            b.Property(x => x.ContentReferenceIds).HasMaxLength(ContentVideoGenerationLimitConsts.ContentReferenceIdsNameMaxLength).IsRequired();

            b.HasIndex(x => new { x.ScopeKey, x.VideoGenerationType });
        });
    }
}