using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.CustomerDomain.Entities;
using Hhs.ContentService.EntityFrameworkCore.Configurations;
using HsnSoft.Base;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Context;

public sealed class ContentServiceDbContext : BaseEfCoreDbContext<ContentServiceDbContext>
{
    public DbSet<CustomerContentSetting> CustomerContentSettings => Set<CustomerContentSetting>();
    public DbSet<CustomerContentSettingPathFilter> CustomerContentSettingPathFilters => Set<CustomerContentSettingPathFilter>();
    public DbSet<CustomerVideoGenerationHistory> CustomerVideoGenerationHistories => Set<CustomerVideoGenerationHistory>();

    public DbSet<AppContent> AppContents => Set<AppContent>();
    public DbSet<AppContentVisit> AppContentVisits => Set<AppContentVisit>();
    public DbSet<AnalysisContent> AnalysisContents => Set<AnalysisContent>();

    // public DbSet<ResponseStatistic> ResponseStatistics => Set<ResponseStatistic>();

    public ContentServiceDbContext(IServiceProvider provider, DbContextOptions<ContentServiceDbContext> options) : base(options, provider)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        base.OnModelCreating(builder);

        builder.ConfigureCustomerContentSettingEntity();
        builder.ConfigureCustomerContentSettingPathFilterEntity();
        builder.ConfigureCustomerVideoGenerationHistoryEntity();

        builder.ConfigureAppContentEntity();
        builder.ConfigureAppContentVisitEntity();
        builder.ConfigureAnalysisContentEntity();

        // builder.ConfigureResponseStatisticEntity();
    }
}