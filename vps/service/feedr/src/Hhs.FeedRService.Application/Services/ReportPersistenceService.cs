using Hhs.FeedRService.Application.Contracts.DashboardDomain;
using Hhs.FeedRService.Application.ReportingDomain;
using Hhs.FeedRService.Domain.ConfigurationDomain.Repositories.MongoDB;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.MongoDB;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;
using Hhs.FeedRService.Domain.ReportingDomain.Enums;
using Hhs.FeedRService.Domain.ReportingDomain.Models;
using Hhs.FeedRService.Domain.ReportingDomain.Repositories.MongoDB;
using Hhs.FeedRService.Domain.ReportingDomain.Repositories.PostgreSQL;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.FeedRService.Application.Services;

public sealed class ReportPersistenceService : ApplicationServiceBase, IReportPersistenceService
{
    private readonly IFrameworkLogger _logger;
    private readonly IRawGoogleAdManagerResponseRepository _rawRepository;
    private readonly IDashboardRepository _dailyRepository;
    private readonly INetworkConfigurationRepository _networkConfigurationRepository;
    private readonly ICustomerConfigurationRepository _customerConfigurationRepository;

    public ReportPersistenceService(
        IServiceProvider provider,
        IRawGoogleAdManagerResponseRepository rawRepository,
        IDashboardRepository dailyRepository,
        INetworkConfigurationRepository networkConfigurationRepository,
        ICustomerConfigurationRepository customerConfigurationRepository) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
        _rawRepository = rawRepository;
        _dailyRepository = dailyRepository;
        _networkConfigurationRepository = networkConfigurationRepository;
        _customerConfigurationRepository = customerConfigurationRepository;
    }

    public async Task<Guid> PersistRawResponseAsync(
        Guid tenantId,
        string requestId,
        string jobName,
        string network,
        string adUnitIdTopLevel,
        string adUnitId,
        DateTime reportDate,
        string rawResponseBody,
        CancellationToken cancellationToken = default)
    {
        var rows = GoogleAdManagerResponseParser.Parse(rawResponseBody);

        var entity = new RawGoogleAdManagerResponse(
            id: Guid.CreateVersion7(),
            tenantId: tenantId == Guid.Empty ? Guid.CreateVersion7() : tenantId,
            requestId: requestId,
            jobName: jobName,
            network: network,
            adUnitIdTopLevel: adUnitIdTopLevel,
            adUnitId: adUnitId,
            reportDate: reportDate,
            rawResponse: rawResponseBody ?? string.Empty,
            rows: rows);

        try
        {
            await _rawRepository.InsertAsync(entity, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Raw Google Ad Manager response persisted to MongoDB. Id={entity.Id}, Rows={rows.Count}",
                reference: new { RawResponseId = entity.Id, RowCount = rows.Count, AdUnitId = adUnitId },
                facility: "REPORT_PERSISTENCE",
                correlationId: requestId,
                exception: null));
        }
        catch (Exception ex)
        {
            _logger.LogError($"[CRITICAL] Failed to insert raw response into MongoDB. Entity.Id={entity.Id}, RequestId={requestId}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            throw;
        }

        return entity.Id;
    }

    public async Task DeriveToPostgresAsync(Guid rawResponseId, CancellationToken cancellationToken = default)
    {
        var raw = await _rawRepository.GetSingleOrDefaultAsync(x => x.Id == rawResponseId, cancellationToken: cancellationToken);
        if (raw == null)
        {
            _logger.LogWarning($"Raw response not found for derivation: {rawResponseId}");
            return;
        }

        await DeriveToPostgresAsync(raw, cancellationToken);
    }

    public async Task<int> DerivePendingReportsAsync(int maxCount = 50, CancellationToken cancellationToken = default)
    {
        var pending = await _rawRepository.GetUnprocessedAsync(maxCount, cancellationToken);
        var count = 0;
        foreach (var raw in pending)
        {
            try
            {
                await DeriveToPostgresAsync(raw, cancellationToken);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to derive raw response {raw.Id}: {ex.Message}");
            }
        }
        return count;
    }

    private async Task DeriveToPostgresAsync(RawGoogleAdManagerResponse raw, CancellationToken cancellationToken)
    {
        await _rawRepository.UpdateProcessedStatusAsync(raw.Id, DerivationStatus.Processing, cancellationToken: cancellationToken);

        var derivedIds = new List<Guid>();

        try
        {
            // Build a lookup of AdUnitCode → (ClientId, ClientName) from the NetworkConfiguration.
            var clientLookup = await BuildClientLookupAsync(raw.Network, raw.AdUnitIdTopLevel, cancellationToken);

            // Ensure hierarchy exists in PostgreSQL.
            // Network and TopLevel are tenant-agnostic; TenantId lives on AdUnitClient and DailyReportResponse.
            var networkDisplayName = clientLookup.NetworkDisplayName;
            var network = await _dailyRepository.EnsureNetworkAsync(raw.Network, networkDisplayName, cancellationToken);
            var topLevel = await _dailyRepository.EnsureTopLevelAsync(network.Id, raw.AdUnitIdTopLevel, cancellationToken);

            // Group rows by (AdUnitId, Date, DemandChannel, DemandSubchannelName, OrderId) —
            // one DailyReportResponse per client-specific AdUnit per day per dimension combination.
            var groups = raw.Rows
                .Where(r => !string.IsNullOrEmpty(r.AdUnitId))
                .GroupBy(r => new { r.AdUnitId, r.Date, r.DemandChannel, r.DemandSubchannelName, r.OrderId, r.OrderName });

            foreach (var group in groups)
            {
                var adUnitCode = group.Key.AdUnitId;
                var demandChannel = group.Key.DemandChannel ?? string.Empty;
                var demandSubchannelName = group.Key.DemandSubchannelName ?? string.Empty;
                var orderId = group.Key.OrderId ?? string.Empty;
                var orderName = group.Key.OrderName ?? string.Empty;

                // Parse the row date (YYYYMMDD) — fall back to raw.ReportDate if missing/invalid.
                var reportDate = DateTime.TryParseExact(group.Key.Date, "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsed)
                    ? parsed
                    : raw.ReportDate;

                // Resolve client info from the lookup — TenantId comes from CustomerConfiguration.
                if (!clientLookup.TryGetValue(adUnitCode, out var info))
                {
                    _logger.LogWarning($"[CLIENT_LOOKUP] AdUnitId '{adUnitCode}' has no client mapping (network={raw.Network}, topLevel={raw.AdUnitIdTopLevel}). Available mapped adUnitCodes=[{string.Join(", ", clientLookup.Clients.Keys)}]. Add this AdUnitId to CustomerConfiguration.AdUnitId in MongoDB.");
                }
                var (tenantId, clientId, clientName) = info == default ? (Guid.Empty, Guid.Empty, string.Empty) : info;

                var clientUnit = await _dailyRepository.EnsureAdUnitClientAsync(
                    tenantId, topLevel.Id, adUnitCode, clientId, clientName, cancellationToken);

                var aggregate = AggregateRows(group.ToList());

                var draft = new DashboardResponse(
                    id: Guid.CreateVersion7(),
                    tenantId: tenantId,
                    adUnitClientId: clientUnit.Id,
                    clientId: clientId,
                    reportDate: reportDate,
                    demandChannel: demandChannel,
                    demandSubchannelName: demandSubchannelName,
                    orderId: orderId,
                    orderName: orderName,
                    codeServedCount: aggregate.CodeServedCount,
                    impressions: aggregate.Impressions,
                    revenue: aggregate.Revenue,
                    activeViewEligibleImpressions: aggregate.ActiveViewEligibleImpressions,
                    averageEcpm: aggregate.AverageEcpm,
                    sourceRowCount: aggregate.SourceRowCount,
                    sourceMongoDbId: raw.Id);

                var persisted = await _dailyRepository.UpsertDailyReportAsync(draft, cancellationToken);
                derivedIds.Add(persisted.Id);
            }

            // Audit trail.
            await _dailyRepository.InsertMappingAsync(
                new MongoToPostgresMapping(Guid.CreateVersion7(), raw.Id, derivedIds, DerivationStatus.Completed),
                cancellationToken);

            await _rawRepository.UpdateProcessedStatusAsync(raw.Id, DerivationStatus.Completed, cancellationToken: cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Derived raw response {raw.Id} into {derivedIds.Count} DailyReportResponse record(s).",
                reference: new { RawResponseId = raw.Id, DerivedCount = derivedIds.Count },
                facility: "REPORT_DERIVATION",
                correlationId: raw.RequestId,
                exception: null));
        }
        catch (Exception ex)
        {
            await _rawRepository.UpdateProcessedStatusAsync(raw.Id, DerivationStatus.Failed, ex.Message, cancellationToken);
            await _dailyRepository.InsertMappingAsync(
                new MongoToPostgresMapping(Guid.CreateVersion7(), raw.Id, derivedIds, DerivationStatus.Failed, ex.Message),
                cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Resolves a map of AdUnitCode → (ClientId, ClientName) from CustomerConfiguration in MongoDB.
    /// NetworkConfiguration is used only to determine the network's DisplayName.
    /// </summary>
    private async Task<ClientLookupResult> BuildClientLookupAsync(
        string networkCode, string adUnitTopLevelCode, CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, (Guid TenantId, Guid ClientId, string ClientName)>(StringComparer.OrdinalIgnoreCase);
        string networkDisplayName = networkCode; // fallback

        // Resolve network display name from NetworkConfiguration
        var networkConfigs = await _networkConfigurationRepository.GetAllActiveAsync();
        var networkConfig = networkConfigs?.FirstOrDefault(n =>
            string.Equals(n.NetworkCode, networkCode, StringComparison.OrdinalIgnoreCase));

        if (networkConfig != null)
        {
            networkDisplayName = networkConfig.DisplayName;
        }
        else
        {
            var availableNetworks = string.Join(", ", networkConfigs?.Select(n => n.NetworkCode) ?? Enumerable.Empty<string>());
            _logger.LogWarning($"[CLIENT_LOOKUP] No NetworkConfiguration found for network={networkCode}. Available networks=[{availableNetworks}]. DisplayName will default to networkCode.");
        }

        // Sole client source: CustomerConfiguration.AdUnitId[] — TenantId comes from CustomerConfiguration
        try
        {
            var customerConfigs = await _customerConfigurationRepository.GetAllAsync(cancellationToken);
            foreach (var cc in customerConfigs)
            {
                if (cc.AdUnitId == null) continue;
                foreach (var adUnitId in cc.AdUnitId)
                {
                    if (!lookup.ContainsKey(adUnitId))
                        lookup[adUnitId] = (cc.TenantId, cc.ClientId, cc.ClientName);
                }
            }

            if (lookup.Count > 0)
            {
                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: $"[CLIENT_LOOKUP] CustomerConfiguration loaded {lookup.Count} AdUnitId mapping(s) for network={networkCode}. adUnitCodes=[{string.Join(", ", lookup.Keys)}]",
                    reference: null, facility: "REPORT_DERIVATION", correlationId: null, exception: null));
            }
            else
            {
                _logger.LogWarning($"[CLIENT_LOOKUP] No client mappings found in CustomerConfiguration for network={networkCode}, topLevel={adUnitTopLevelCode}. Ensure CustomerConfiguration documents contain the correct AdUnitId values.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[CLIENT_LOOKUP] CustomerConfiguration lookup failed: {ex.Message}");
        }

        return new ClientLookupResult(lookup, networkDisplayName);
    }

    private sealed record ClientLookupResult(
        Dictionary<string, (Guid TenantId, Guid ClientId, string ClientName)> Clients,
        string NetworkDisplayName)
    {
        public bool TryGetValue(string adUnitCode, out (Guid TenantId, Guid ClientId, string ClientName) info)
            => Clients.TryGetValue(adUnitCode, out info);
    }

    private static AggregatedRow AggregateRows(List<GoogleAdManagerReportRow> rows)
    {
        long codeServed = 0, impressions = 0, activeView = 0;
        double revenue = 0d;
        double weightedEcpmNumerator = 0d;

        foreach (var r in rows)
        {
            codeServed += r.CodeServedCount;
            impressions += r.Impressions;
            activeView += r.ActiveViewEligibleImpressions;
            revenue += r.Revenue;
            weightedEcpmNumerator += r.AverageEcpm * r.Impressions;
        }

        double averageEcpm = impressions > 0 ? weightedEcpmNumerator / impressions : 0d;

        return new AggregatedRow
        {
            CodeServedCount = codeServed,
            Impressions = impressions,
            Revenue = revenue,
            ActiveViewEligibleImpressions = activeView,
            AverageEcpm = averageEcpm,
            SourceRowCount = rows.Count
        };
    }

    private sealed class AggregatedRow
    {
        public long CodeServedCount { get; set; }
        public long Impressions { get; set; }
        public double Revenue { get; set; }
        public long ActiveViewEligibleImpressions { get; set; }
        public double AverageEcpm { get; set; }
        public int SourceRowCount { get; set; }
    }
}
