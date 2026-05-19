using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.DashboardDomain.Entities;
using Hhs.ContentService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Context;

public sealed class ContentServiceDbContext : BaseEfCoreDbContext<ContentServiceDbContext>
{
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientPathFilter> ClientPathFilters { get; set; }
    public DbSet<ClientVideoGenerationHistory> ClientVideoGenerationHistories { get; set; }

    public DbSet<AppContent> AppContents { get; set; }
    public DbSet<AppContentVisit> AppContentVisits { get; set; }

    public DbSet<AnalysisContent> AnalysisContents { get; set; }
    public DbSet<ResponseStatistic> ResponseStatistics { get; set; }

    public ContentServiceDbContext(IServiceProvider provider, DbContextOptions<ContentServiceDbContext> options) : base(options, provider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureClientEntity();
        modelBuilder.ConfigureClientPathFilterEntity();
        modelBuilder.ConfigureClientVideoGenerationHistoryEntity();
        modelBuilder.ConfigureAppContentEntity();
        modelBuilder.ConfigureAppContentVisitEntity();
        modelBuilder.ConfigureAnalysisContentEntity();
        modelBuilder.ConfigureResponseStatisticEntity();
    }
}