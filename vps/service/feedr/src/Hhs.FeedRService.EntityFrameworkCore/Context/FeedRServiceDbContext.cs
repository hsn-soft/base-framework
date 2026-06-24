using Hhs.FeedRService.Domain.InfraDomain.Entities.PostgreSQL;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;
using Hhs.FeedRService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.FeedRService.EntityFrameworkCore.Context;

public sealed class FeedRServiceDbContext(IServiceProvider provider, DbContextOptions<FeedRServiceDbContext> options)
    : BaseEfCoreDbContext<FeedRServiceDbContext>(options, provider)
{
    public DbSet<EventInboxMessage> EventInboxMessages { get; set; }
    public DbSet<AdNetwork> AdNetworks { get; set; }
    public DbSet<AdUnitTopLevel> AdUnitTopLevels { get; set; }
    public DbSet<AdUnitClient> AdUnitClients { get; set; }
    public DbSet<DashboardResponse> DashboardResponses { get; set; }
    public DbSet<MongoToPostgresMapping> MongoToPostgresMappings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureEventInboxMessageEntity();
        modelBuilder.ConfigureReportingEntities();
    }
}