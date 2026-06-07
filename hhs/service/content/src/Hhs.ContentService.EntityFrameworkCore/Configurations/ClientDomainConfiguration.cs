using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.ClientDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class ClientDomainConfiguration
{
    public static void ConfigureClientEntity(this ModelBuilder builder)
    {
        builder.Entity<Client>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ClientConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.DomainName).IsRequired().HasMaxLength(ClientConsts.DomainNameMaxLength);
            b.Property(x => x.IsBlocked).IsRequired();

            b.Property(x => x.DailyDirectVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyDirectVideoGenerationLimit).IsRequired();

            b.Property(x => x.DailyTrendVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyTrendVideoGenerationLimit).IsRequired();
            b.Property(x => x.DailyTrendVideoWaitStatisticHour).IsRequired();
            b.Property(x => x.DailyTrendVideoMinVisitCount).IsRequired();

            b.Property(x => x.DailyAnalysisVideoGenerationStartedUtcHour).IsRequired();
            b.Property(x => x.DailyAnalysisVideoGenerationLimit).IsRequired();

            b.HasIndex(x => x.IsDeleted);
            b.HasIndex(x => new { x.TenantId, x.DomainName })
                .IsUnique();
        });
    }

    public static void ConfigureClientPathFilterEntity(this ModelBuilder builder)
    {
        builder.Entity<ClientPathFilter>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ClientPathFilterConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).IsRequired();
            b.Property(x => x.PathFilterName).IsRequired().HasMaxLength(ClientPathFilterConsts.PathFilterNameMaxLength);

            b.HasOne(x => x.Client)
                .WithMany(x => x.PathFilters)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }

    public static void ConfigureClientVideoGenerationHistoryEntity(this ModelBuilder builder)
    {
        builder.Entity<ClientVideoGenerationHistory>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ClientVideoGenerationHistoryConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).IsRequired();
            b.Property(x => x.VideoGenerationDate).IsRequired();
            b.Property(x => x.VideoGenerationType).IsRequired();
            b.Property(x => x.ContentReferenceIds).IsRequired().HasMaxLength(ClientVideoGenerationHistoryConsts.ContentReferenceIdsNameMaxLength);

            b.HasOne(x => x.Client)
                .WithMany(x => x.VideoGenerationHistories)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasIndex(x => new { x.ClientId, x.VideoGenerationType });
        });
    }
}