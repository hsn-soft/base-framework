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
            b.Property(x => x.DomainName).HasMaxLength(CustomerContentConsts.DomainNameMaxLength).IsRequired();
            b.Property(x => x.ContentKey).HasMaxLength(CustomerContentConsts.ContentKeyMaxLength).IsRequired();
            b.Property(x => x.SlugKey).HasMaxLength(CustomerContentConsts.SlugKeyMaxLength).IsRequired();
            b.Property(x => x.CorrelationId).HasMaxLength(CustomerContentConsts.CorrelationIdMaxLength);
            b.Property(x => x.NormalizeStatus).HasMaxLength(CustomerContentConsts.NormalizeStatusMaxLength);
            b.Property(x => x.VideoStatus).HasMaxLength(CustomerContentConsts.VideoStatusMaxLength);
            b.Property(x => x.FinalVideoUrl).HasMaxLength(CustomerContentConsts.FinalVideoUrlMaxLength);
            b.Property(x => x.LastFacility).HasMaxLength(CustomerContentConsts.LastFacilityMaxLength);
            b.Property(x => x.LastError).HasMaxLength(CustomerContentConsts.LastErrorMaxLength);

            b.HasIndex(x => x.ScopeKey);
            b.HasIndex(x => new { x.ScopeKey, x.DomainName }).IsUnique();
        });
    }

    public static void ConfigureAnalysisContentEntity(this ModelBuilder builder)
    {
        builder.Entity<AnalysisContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AnalysisContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(AnalysisContentConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.DomainName).HasMaxLength(AnalysisContentConsts.DomainNameMaxLength).IsRequired();
            b.Property(x => x.Title).HasMaxLength(AnalysisContentConsts.TitleMaxLength);
            b.Property(x => x.CorrelationId).HasMaxLength(AnalysisContentConsts.CorrelationIdMaxLength);
            b.Property(x => x.NormalizeStatus).HasMaxLength(AnalysisContentConsts.NormalizeStatusMaxLength);
            b.Property(x => x.VideoStatus).HasMaxLength(AnalysisContentConsts.VideoStatusMaxLength);
            b.Property(x => x.FinalVideoUrl).HasMaxLength(AnalysisContentConsts.FinalVideoUrlMaxLength);
            b.Property(x => x.LastFacility).HasMaxLength(AnalysisContentConsts.LastFacilityMaxLength);
            b.Property(x => x.LastError).HasMaxLength(AnalysisContentConsts.LastErrorMaxLength);

            b.HasIndex(x => x.ScopeKey);
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