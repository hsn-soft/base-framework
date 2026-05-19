using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.DashboardDomain.Consts;
using Hhs.ContentService.Domain.DashboardDomain.Entities;
using HsnSoft.Base;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class ContentConfiguration
{
    public static void ConfigureAppContentEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AppContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).HasColumnName(nameof(AppContent.ClientId)).IsRequired();
            b.Property(x => x.SlugKey).HasColumnName(nameof(AppContent.SlugKey)).IsRequired().HasMaxLength(AppContentConsts.SlugKeyMaxLength);
            b.Property(x => x.OperationStatus).HasColumnName(nameof(AppContent.OperationStatus)).IsRequired();
            b.Property(x => x.OperationStatusDescription).HasColumnName(nameof(AppContent.OperationStatusDescription));
            b.Property(x => x.NormalizedRequestId).HasColumnName(nameof(AppContent.NormalizedRequestId));
            b.Property(x => x.ReleaseTime).HasColumnName(nameof(AppContent.ReleaseTime));
            b.Property(x => x.VideoRequestId).HasColumnName(nameof(AppContent.VideoRequestId));
            b.Property(x => x.StorageVideoUrl).HasColumnName(nameof(AppContent.StorageVideoUrl));
            b.Property(x => x.CorrelationId).HasColumnName(nameof(AppContent.CorrelationId));

            b.HasIndex(x => new { x.IsDeleted });
            b.HasIndex(x => new { x.TenantId });

            b.HasIndex(x => new { x.OperationStatus });
            b.HasIndex(x => new { x.ClientId, x.SlugKey });

            b.HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.ClientId).OnDelete(deleteBehavior: DeleteBehavior.Restrict) //no cascade delete
                .IsRequired();

            b.Navigation(x => x.Client);
        });
    }

    public static void ConfigureAppContentVisitEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AppContentVisit>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppContentVisitConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).HasColumnName(nameof(AppContentVisit.ClientId)).IsRequired();
            b.Property(x => x.AppContentId).HasColumnName(nameof(AppContentVisit.AppContentId)).IsRequired();
            b.Property(x => x.VisitTimeLine).HasColumnName(nameof(AppContentVisit.VisitTimeLine)).IsRequired();
            b.Property(x => x.VisitResponse).HasColumnName(nameof(AppContentVisit.VisitResponse)).IsRequired().HasMaxLength(AppContentVisitConsts.VisitResponseMaxLength);

            b.HasIndex(x => new { x.ClientId, x.AppContentId });
            b.HasIndex(x => new { x.ClientId, x.VisitTimeLine });
            b.HasIndex(x => new { x.ClientId, x.VisitResponse });
        });
    }

    public static void ConfigureAnalysisContentEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AnalysisContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AnalysisContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).HasColumnName(nameof(AnalysisContent.ClientId)).IsRequired();
            b.Property(x => x.AnalysisDate).HasColumnName(nameof(AnalysisContent.AnalysisDate)).IsRequired();
            b.Property(x => x.OperationStatus).HasColumnName(nameof(AnalysisContent.OperationStatus)).IsRequired();
            b.Property(x => x.OperationStatusDescription).HasColumnName(nameof(AnalysisContent.OperationStatusDescription));
            b.Property(x => x.NormalizedAnalysisId).HasColumnName(nameof(AnalysisContent.NormalizedAnalysisId));
            b.Property(x => x.VideoRequestId).HasColumnName(nameof(AnalysisContent.VideoRequestId));
            b.Property(x => x.StorageVideoUrl).HasColumnName(nameof(AnalysisContent.StorageVideoUrl));
            b.Property(x => x.CorrelationId).HasColumnName(nameof(AnalysisContent.CorrelationId));

            b.HasIndex(x => new { x.IsDeleted });
            b.HasIndex(x => new { x.TenantId });

            b.HasIndex(x => new { x.OperationStatus });

            b.HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.ClientId).OnDelete(deleteBehavior: DeleteBehavior.Restrict) //no cascade delete
                .IsRequired();

            b.Navigation(x => x.Client);
        });
    }

    public static void ConfigureClientEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<Client>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ClientConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.SubdomainName).HasColumnName(nameof(Client.SubdomainName)).HasMaxLength(ClientConsts.SubdomainNameMaxLength);
            b.Property(x => x.DomainName).HasColumnName(nameof(Client.DomainName)).IsRequired().HasMaxLength(ClientConsts.DomainNameMaxLength);
            b.Property(x => x.IsBlocked).HasColumnName(nameof(Client.IsBlocked)).IsRequired();

            b.Property(x => x.DailyDirectVideoGenerationStartedUtcHour).HasColumnName(nameof(Client.DailyDirectVideoGenerationStartedUtcHour)).IsRequired();
            b.Property(x => x.DailyDirectVideoGenerationLimit).HasColumnName(nameof(Client.DailyDirectVideoGenerationLimit)).IsRequired();

            b.Property(x => x.DailyTrendVideoGenerationStartedUtcHour).HasColumnName(nameof(Client.DailyTrendVideoGenerationStartedUtcHour)).IsRequired();
            b.Property(x => x.DailyTrendVideoGenerationLimit).HasColumnName(nameof(Client.DailyTrendVideoGenerationLimit)).IsRequired();
            b.Property(x => x.DailyTrendVideoWaitStatisticHour).HasColumnName(nameof(Client.DailyTrendVideoWaitStatisticHour)).IsRequired();
            b.Property(x => x.DailyTrendVideoMinVisitCount).HasColumnName(nameof(Client.DailyTrendVideoMinVisitCount)).IsRequired();

            b.Property(x => x.DailyAnalysisVideoGenerationStartedUtcHour).HasColumnName(nameof(Client.DailyAnalysisVideoGenerationStartedUtcHour)).IsRequired();
            b.Property(x => x.DailyAnalysisVideoGenerationLimit).HasColumnName(nameof(Client.DailyAnalysisVideoGenerationLimit)).IsRequired();

            b.HasIndex(x => new { x.IsDeleted });
            b.HasIndex(x => new { x.TenantId });

            b.HasIndex(m => m.DomainName).IsUnique();

            b.Navigation(x => x.PathFilters);
        });
    }

    public static void ConfigureClientPathFilterEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<ClientPathFilter>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ClientPathFilterConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).HasColumnName(nameof(ClientPathFilter.ClientId)).IsRequired();
            b.Property(x => x.PathFilterName).HasColumnName(nameof(ClientPathFilter.PathFilterName)).IsRequired().HasMaxLength(ClientPathFilterConsts.PathFilterNameMaxLength);

            b.HasOne<Client>()
                .WithMany(x => x.PathFilters)
                .HasForeignKey(s => s.ClientId).OnDelete(deleteBehavior: DeleteBehavior.Cascade)
                .IsRequired();
        });
    }

    public static void ConfigureClientVideoGenerationHistoryEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<ClientVideoGenerationHistory>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ClientVideoGenerationHistoryConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).HasColumnName(nameof(ClientVideoGenerationHistory.ClientId)).IsRequired();
            b.Property(x => x.VideoGenerationDate).HasColumnName(nameof(ClientVideoGenerationHistory.VideoGenerationDate)).IsRequired();
            b.Property(x => x.VideoGenerationType).HasColumnName(nameof(ClientVideoGenerationHistory.VideoGenerationType)).IsRequired();
            b.Property(x => x.ContentReferenceIds).HasColumnName(nameof(ClientVideoGenerationHistory.ContentReferenceIds)).IsRequired();

            b.HasOne<Client>()
                .WithMany(x => x.VideoGenerationHistories)
                .HasForeignKey(s => s.ClientId).OnDelete(deleteBehavior: DeleteBehavior.Cascade)
                .IsRequired();
        });
    }

    public static void ConfigureResponseStatisticEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<ResponseStatistic>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + ResponseStatisticConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).HasColumnName(nameof(ResponseStatistic.ClientId)).IsRequired();
            b.Property(x => x.ResponseStatus).HasColumnName(nameof(ResponseStatistic.ResponseStatus)).IsRequired().HasMaxLength(ResponseStatisticConsts.ResponseStatusMaxLength);
            b.Property(x => x.ResponseTime).HasColumnName(nameof(ResponseStatistic.ResponseTime)).IsRequired();
            b.Property(x => x.ResponseCount).HasColumnName(nameof(ResponseStatistic.ResponseCount)).IsRequired();


            b.HasIndex(x => new { x.TenantId });

            b.HasIndex(x => new { x.ClientId, x.ResponseStatus });
            b.HasIndex(x => new { x.ClientId, x.ResponseTime });
        });
    }
}