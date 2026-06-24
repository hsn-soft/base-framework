using Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;
using Hhs.FeedRService.Domain.ReportingDomain.Repositories.PostgreSQL;
using Hhs.FeedRService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Hhs.FeedRService.EntityFrameworkCore.Repositories.PostgreSQL;

public sealed class EfCoreDashboardRepository(
    IServiceProvider provider,
    FeedRServiceDbContext dbContext)
    : EfCoreGenericRepository<DashboardResponse, Guid>(provider, dbContext), IDashboardRepository
{
    private readonly FeedRServiceDbContext _dbContext = dbContext;

    public async Task<AdNetwork> EnsureNetworkAsync(string networkCode, string displayName, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.AdNetworks.FirstOrDefaultAsync(
            x => x.NetworkCode == networkCode, cancellationToken);

        if (existing != null)
        {
            // Backfill DisplayName if it was previously empty or just the code
            if (!string.IsNullOrEmpty(displayName) && existing.DisplayName != displayName)
            {
                existing.UpdateDisplayName(displayName);
                _dbContext.AdNetworks.Update(existing);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return existing;
        }

        var draft = new AdNetwork(Guid.NewGuid(), networkCode, displayName);
        await _dbContext.AdNetworks.AddAsync(draft, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<AdUnitTopLevel> EnsureTopLevelAsync(Guid adNetworkId, string adUnitTopLevelCode, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.AdUnitTopLevels.FirstOrDefaultAsync(
            x => x.AdNetworkId == adNetworkId && x.AdUnitTopLevelCode == adUnitTopLevelCode, cancellationToken);

        if (existing != null) return existing;

        var draft = new AdUnitTopLevel(Guid.NewGuid(), adNetworkId, adUnitTopLevelCode);
        await _dbContext.AdUnitTopLevels.AddAsync(draft, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<AdUnitClient> EnsureAdUnitClientAsync(Guid tenantId, Guid adUnitTopLevelId, string adUnitCode, Guid clientId, string clientName, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.AdUnitClients.FirstOrDefaultAsync(
            x => x.AdUnitTopLevelId == adUnitTopLevelId && x.AdUnitCode == adUnitCode, cancellationToken);

        if (existing != null)
        {
            // Backfill ClientId/ClientName if they were previously unknown (Guid.Empty) and we now have real data
            bool needsUpdate = (existing.ClientId == Guid.Empty && clientId != Guid.Empty)
                               || (string.IsNullOrEmpty(existing.ClientName) && !string.IsNullOrEmpty(clientName));
            if (needsUpdate)
            {
                existing.UpdateClientMetadata(
                    clientId != Guid.Empty ? clientId : existing.ClientId,
                    !string.IsNullOrEmpty(clientName) ? clientName : existing.ClientName);
                _dbContext.AdUnitClients.Update(existing);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return existing;
        }

        var draft = new AdUnitClient(Guid.NewGuid(), tenantId, adUnitTopLevelId, adUnitCode, clientId, clientName);
        await _dbContext.AdUnitClients.AddAsync(draft, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<DashboardResponse> UpsertDailyReportAsync(DashboardResponse draft, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DashboardResponses.FirstOrDefaultAsync(
            x => x.AdUnitClientId == draft.AdUnitClientId && 
                 x.ReportDate == draft.ReportDate &&
                 x.DemandChannel == draft.DemandChannel &&
                 x.DemandSubchannelName == draft.DemandSubchannelName &&
                 x.OrderId == draft.OrderId, 
            cancellationToken);

        if (existing == null)
        {
            await _dbContext.DashboardResponses.AddAsync(draft, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return draft;
        }

        existing.MergeFrom(
            draft.CodeServedCount,
            draft.Impressions,
            draft.Revenue,
            draft.ActiveViewEligibleImpressions,
            draft.AverageEcpm,
            draft.SourceRowCount,
            draft.SourceMongoDbId);

        _dbContext.DashboardResponses.Update(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task InsertMappingAsync(MongoToPostgresMapping mapping, CancellationToken cancellationToken = default)
    {
        await _dbContext.MongoToPostgresMappings.AddAsync(mapping, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DashboardResponse>> GetByAdUnitAndDateRangeAsync(string adUnitCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var from = startDate.Date;
        var to = endDate.Date;

        return await _dbContext.DashboardResponses
            .Include(x => x.AdUnitClient)
            .Where(x => x.AdUnitClient.AdUnitCode == adUnitCode && x.ReportDate >= from && x.ReportDate <= to)
            .OrderBy(x => x.ReportDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DashboardResponse>> GetByClientAndDateRangeAsync(Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var from = startDate.Date;
        var to = endDate.Date;

        return await _dbContext.DashboardResponses
            .Include(x => x.AdUnitClient)
            .Where(x => x.ClientId == clientId && x.ReportDate >= from && x.ReportDate <= to)
            .OrderBy(x => x.ReportDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DashboardResponse>> GetByNetworkAndDateRangeAsync(string networkCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var fromDate = startDate.Date;
        var toDate = endDate.Date;

        return await (
            from dr in _dbContext.DashboardResponses.Include(x => x.AdUnitClient)
            join client in _dbContext.AdUnitClients on dr.AdUnitClientId equals client.Id
            join topLevel in _dbContext.AdUnitTopLevels on client.AdUnitTopLevelId equals topLevel.Id
            join network in _dbContext.AdNetworks on topLevel.AdNetworkId equals network.Id
            where network.NetworkCode == networkCode && dr.ReportDate >= fromDate && dr.ReportDate <= toDate
            orderby dr.ReportDate
            select dr
        ).ToListAsync(cancellationToken);
    }

    public async Task<List<DashboardResponse>> GetByTopLevelAndDateRangeAsync(string adUnitTopLevelCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var fromDate = startDate.Date;
        var toDate = endDate.Date;

        return await (
            from dr in _dbContext.DashboardResponses.Include(x => x.AdUnitClient)
            join client in _dbContext.AdUnitClients on dr.AdUnitClientId equals client.Id
            join topLevel in _dbContext.AdUnitTopLevels on client.AdUnitTopLevelId equals topLevel.Id
            where topLevel.AdUnitTopLevelCode == adUnitTopLevelCode && dr.ReportDate >= fromDate && dr.ReportDate <= toDate
            orderby dr.ReportDate
            select dr
        ).ToListAsync(cancellationToken);
    }

    public async Task<List<DashboardResponse>> GetByCompositeFilterAsync(string networkCode, string adUnitTopLevelCode, Guid? clientId, string demandChannel, string demandSubchannelName, string orderId, string orderName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var fromDate = startDate.Date;
        var toDate = endDate.Date;

        var query =
            from dr in _dbContext.DashboardResponses.Include(x => x.AdUnitClient)
            join client in _dbContext.AdUnitClients on dr.AdUnitClientId equals client.Id
            join topLevel in _dbContext.AdUnitTopLevels on client.AdUnitTopLevelId equals topLevel.Id
            join network in _dbContext.AdNetworks on topLevel.AdNetworkId equals network.Id
            where dr.ReportDate >= fromDate && dr.ReportDate <= toDate
            select new { dr, client, topLevel, network };

        if (!string.IsNullOrEmpty(networkCode))
            query = query.Where(x => x.network.NetworkCode == networkCode);

        if (!string.IsNullOrEmpty(adUnitTopLevelCode))
            query = query.Where(x => x.topLevel.AdUnitTopLevelCode == adUnitTopLevelCode);

        if (clientId.HasValue && clientId.Value != Guid.Empty)
            query = query.Where(x => x.dr.ClientId == clientId.Value);

        if (!string.IsNullOrEmpty(demandChannel))
            query = query.Where(x => x.dr.DemandChannel == demandChannel);
        
        if (!string.IsNullOrEmpty(demandSubchannelName))
            query = query.Where(x => x.dr.DemandSubchannelName == demandSubchannelName);

        if (!string.IsNullOrEmpty(orderId))
            query = query.Where(x => x.dr.OrderId == orderId);

        if (!string.IsNullOrEmpty(orderName))   
            query = query.Where(x => x.dr.OrderName == orderName);

        return await query.OrderBy(x => x.dr.ReportDate).Select(x => x.dr).ToListAsync(cancellationToken);
    }

    public async Task<List<DashboardResponse>> GetByDemandChannelFilterAsync(
        string networkCode,
        string adUnitTopLevelCode,
        string demandChannel,
        string demandSubchannelName,
        Guid? clientId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var fromDate = startDate.Date;
        var toDate = endDate.Date;

        var query =
            from dr in _dbContext.DashboardResponses.Include(x => x.AdUnitClient)
            join client in _dbContext.AdUnitClients on dr.AdUnitClientId equals client.Id
            join topLevel in _dbContext.AdUnitTopLevels on client.AdUnitTopLevelId equals topLevel.Id
            join network in _dbContext.AdNetworks on topLevel.AdNetworkId equals network.Id
            where dr.ReportDate >= fromDate && dr.ReportDate <= toDate &&
                  dr.DemandChannel == demandChannel && 
                  dr.DemandSubchannelName == demandSubchannelName
            select new { dr, client, topLevel, network };

        if (!string.IsNullOrEmpty(networkCode))
            query = query.Where(x => x.network.NetworkCode == networkCode);

        if (!string.IsNullOrEmpty(adUnitTopLevelCode))
            query = query.Where(x => x.topLevel.AdUnitTopLevelCode == adUnitTopLevelCode);

        if (clientId.HasValue && clientId.Value != Guid.Empty)
            query = query.Where(x => x.dr.ClientId == clientId.Value);

        return await query.OrderBy(x => x.dr.ReportDate).Select(x => x.dr).ToListAsync(cancellationToken);
    }

    public async Task<List<DashboardResponse>> GetByOrderFilterAsync(
        string networkCode,
        string orderId,
        string adUnitCode,
        Guid? clientId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var fromDate = startDate.Date;
        var toDate = endDate.Date;

        var query =
            from dr in _dbContext.DashboardResponses.Include(x => x.AdUnitClient)
            join client in _dbContext.AdUnitClients on dr.AdUnitClientId equals client.Id
            join topLevel in _dbContext.AdUnitTopLevels on client.AdUnitTopLevelId equals topLevel.Id
            join network in _dbContext.AdNetworks on topLevel.AdNetworkId equals network.Id
            where dr.ReportDate >= fromDate && dr.ReportDate <= toDate &&
                  dr.OrderId == orderId
            select new { dr, client, topLevel, network };

        if (!string.IsNullOrEmpty(networkCode))
            query = query.Where(x => x.network.NetworkCode == networkCode);

        if (!string.IsNullOrEmpty(adUnitCode))
            query = query.Where(x => x.client.AdUnitCode == adUnitCode);

        if (clientId.HasValue && clientId.Value != Guid.Empty)
            query = query.Where(x => x.dr.ClientId == clientId.Value);

        return await query.OrderBy(x => x.dr.ReportDate).Select(x => x.dr).ToListAsync(cancellationToken);
    }

    public async Task<List<(string OrderId, string OrderName)>> GetDistinctOrdersAsync(
        [CanBeNull] string networkCode,
        [CanBeNull] string adUnitTopLevelCode,
        [CanBeNull] string adUnitCode,
        Guid? clientId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from dr in _dbContext.DashboardResponses
            join client in _dbContext.AdUnitClients on dr.AdUnitClientId equals client.Id
            join topLevel in _dbContext.AdUnitTopLevels on client.AdUnitTopLevelId equals topLevel.Id
            join network in _dbContext.AdNetworks on topLevel.AdNetworkId equals network.Id
            where dr.OrderId != string.Empty
            select new { dr.OrderId, dr.OrderName, client, topLevel, network };

        if (!string.IsNullOrEmpty(networkCode))
            query = query.Where(x => x.network.NetworkCode == networkCode);

        if (!string.IsNullOrEmpty(adUnitTopLevelCode))
            query = query.Where(x => x.topLevel.AdUnitTopLevelCode == adUnitTopLevelCode);

        if (!string.IsNullOrEmpty(adUnitCode))
            query = query.Where(x => x.client.AdUnitCode == adUnitCode);

        if (clientId.HasValue && clientId.Value != Guid.Empty)
            query = query.Where(x => x.client.ClientId == clientId.Value);

        var results = await query
            .Select(x => new { x.OrderId, x.OrderName })
            .Distinct()
            .OrderBy(x => x.OrderName)
            .ToListAsync(cancellationToken);

        return results
            .Select(x => (x.OrderId, x.OrderName))
            .ToList();
    }
}
