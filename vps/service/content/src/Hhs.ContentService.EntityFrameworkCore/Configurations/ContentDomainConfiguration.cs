using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class ContentDomainConfiguration
{
    public static void ConfigureCustomerContentEntity(this ModelBuilder builder)
    {
        builder.Entity<CustomerContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(CustomerContentConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.ContentKey).HasMaxLength(CustomerContentConsts.ContentKeyMaxLength).IsRequired();
            b.Property(x => x.SlugKey).HasMaxLength(CustomerContentConsts.ContentKeyMaxLength).IsRequired();

            b.Property(x => x.CorrelationId).HasMaxLength(CustomerContentConsts.CorrelationIdMaxLength);

            b.Property(x => x.NormalizeStatus).HasMaxLength(CustomerContentConsts.NormalizeStatusMaxLength);
            b.Property(x => x.NormalizeRequestId);
            b.Property(x => x.ScrapReleaseTimeUtc);

            b.Property(x => x.VideoStatus).HasMaxLength(CustomerContentConsts.VideoStatusMaxLength);
            b.Property(x => x.AudioRequestId);
            b.Property(x => x.VideoRequestId);
            b.Property(x => x.VideoCdnUrl).HasMaxLength(CustomerContentConsts.VideoCdnUrlMaxLength);

            b.Property(x => x.LastFacility).HasMaxLength(CustomerContentConsts.LastFacilityMaxLength);
            b.Property(x => x.LastError).HasMaxLength(CustomerContentConsts.LastErrorMaxLength);

            b.HasIndex(x => x.ScopeKey);
            b.HasIndex(x => new { x.ScopeKey, x.ContentKey }).IsUnique();
        });
    }

    public static void ConfigureCustomerContentVisitEntity(this ModelBuilder builder)
    {
        builder.Entity<CustomerContentVisit>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerContentVisitConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(CustomerContentVisitConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.CustomerContentId).IsRequired();
            b.Property(x => x.VisitTime).IsRequired();
            b.Property(x => x.VisitResponse).IsRequired().HasMaxLength(CustomerContentVisitConsts.VisitResponseMaxLength);

            b.HasIndex(x => x.ScopeKey);
            b.HasIndex(x => x.CustomerContentId);
            b.HasIndex(x => x.VisitTime);
            b.HasIndex(x => x.VisitResponse);
        });
    }

    public static void ConfigureAnalysisContentEntity(this ModelBuilder builder)
    {
        builder.Entity<AnalysisContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AnalysisContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(AnalysisContentConsts.ScopeKeyMaxLength).IsRequired();

            b.Property(x => x.AnalysisDate).IsRequired();

            b.Property(x => x.CorrelationId).HasMaxLength(AnalysisContentConsts.CorrelationIdMaxLength);

            b.Property(x => x.NormalizeStatus).HasMaxLength(AnalysisContentConsts.NormalizeStatusMaxLength);
            b.Property(x => x.NormalizeRequestId);

            b.Property(x => x.VideoStatus).HasMaxLength(AnalysisContentConsts.VideoStatusMaxLength);
            b.Property(x => x.VideoRequestId);
            b.Property(x => x.VideoCdnUrl).HasMaxLength(AnalysisContentConsts.FinalVideoUrlMaxLength);

            b.Property(x => x.LastFacility).HasMaxLength(AnalysisContentConsts.LastFacilityMaxLength);
            b.Property(x => x.LastError).HasMaxLength(AnalysisContentConsts.LastErrorMaxLength);

            b.HasIndex(x => x.ScopeKey);
            b.HasIndex(x => x.AnalysisDate);
            b.HasIndex(x => x.VideoStatus);
        });
    }

    public static void ConfigureAnalysisContentItemEntity(this ModelBuilder builder)
    {
        builder.Entity<AnalysisContentItem>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AnalysisContentItemConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.AnalysisContentId, x.CustomerContentId }).IsUnique();
            b.HasIndex(x => new { x.AnalysisContentId, x.SortOrder }).IsUnique();

            b.HasOne(x => x.AnalysisContent)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.AnalysisContentId);

            b.HasOne(x => x.CustomerContent)
                .WithMany()
                .HasForeignKey(x => x.CustomerContentId);
        });
    }

}