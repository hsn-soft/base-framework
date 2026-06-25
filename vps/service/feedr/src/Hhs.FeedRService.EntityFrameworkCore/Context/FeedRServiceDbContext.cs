using Hhs.FeedRService.Domain.InfraDomain.Entities.PostgreSQL;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;
using Hhs.FeedRService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.FeedRService.EntityFrameworkCore.Context;

public sealed class FeedRServiceDbContext(IServiceProvider provider, DbContextOptions<FeedRServiceDbContext> options)
    : BaseEfCoreDbContext<FeedRServiceDbContext>(options, provider)
{
    public DbSet<EventInboxMessage> EventInboxMessages => Set<EventInboxMessage>();
    public DbSet<AdNetwork> AdNetworks => Set<AdNetwork>();
    public DbSet<AdUnitTopLevel> AdUnitTopLevels => Set<AdUnitTopLevel>();
    public DbSet<AdUnitClient> AdUnitClients => Set<AdUnitClient>();
    public DbSet<DashboardResponse> DashboardResponses => Set<DashboardResponse>();
    public DbSet<MongoToPostgresMapping> MongoToPostgresMappings => Set<MongoToPostgresMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureEventInboxMessageEntity();
        modelBuilder.ConfigureReportingEntities();
    }
}