using Hhs.FeedRService.Application.Contracts.DashboardDomain;
using Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;
using Hhs.FeedRService.Domain.ReportingDomain.Repositories.PostgreSQL;

namespace Hhs.FeedRService.Application.Services;

public sealed class ReportQueryService : ApplicationServiceBase, IReportQueryService
{
    private readonly IDashboardRepository _dailyRepository;

    public ReportQueryService(
        IServiceProvider provider,
        IDashboardRepository dailyRepository) : base(provider)
    {
        _dailyRepository = dailyRepository;
    }

    public async Task<AggregatedReportResultDto> GetByAdUnitAndDateRangeAsync(GetReportByAdUnitRangeDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByAdUnitAndDateRangeAsync(input.AdUnitId, input.StartDate, input.EndDate, cancellationToken);
        return Aggregate(records, input.AdUnitId, null, input.StartDate, input.EndDate);
    }

    public async Task<AggregatedReportResultDto> GetByClientAndDateRangeAsync(GetReportByClientRangeDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByClientAndDateRangeAsync(input.ClientId, input.StartDate, input.EndDate, cancellationToken);
        return Aggregate(records, null, input.ClientId, input.StartDate, input.EndDate);
    }

    public async Task<AggregatedReportResultDto> GetByNetworkAndDateRangeAsync(GetReportByNetworkRangeDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByNetworkAndDateRangeAsync(input.NetworkCode, input.StartDate, input.EndDate, cancellationToken);
        return Aggregate(records, null, null, input.StartDate, input.EndDate);
    }

    public async Task<AggregatedReportResultDto> GetByTopLevelAndDateRangeAsync(GetReportByTopLevelRangeDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByTopLevelAndDateRangeAsync(input.AdUnitTopLevelCode, input.StartDate, input.EndDate, cancellationToken);
        return Aggregate(records, null, null, input.StartDate, input.EndDate);
    }

    public async Task<AggregatedReportResultDto> GetByCompositeFilterAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByCompositeFilterAsync(input.NetworkCode, input.AdUnitTopLevelCode, input.ClientId, input.DemandChannel, input.DemandSubchannelName, input.OrderId, input.OrderName, input.StartDate, input.EndDate, cancellationToken);
        return Aggregate(records, null, input.ClientId, input.StartDate, input.EndDate, input.IncludeRecordBreakdown);
    }

    public async Task<AggregatedReportResultDto> GetByDemandChannelFilterAsync(GetReportByDemandChannelFilterDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByDemandChannelFilterAsync(
            input.NetworkCode,
            input.AdUnitTopLevelCode,
            input.DemandChannel,
            input.DemandSubchannelName,
            input.ClientId,
            input.StartDate,
            input.EndDate,
            cancellationToken);
        return Aggregate(records, null, input.ClientId, input.StartDate, input.EndDate);
    }

    public async Task<AggregatedReportResultDto> GetByOrderFilterAsync(GetReportByOrderFilterDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByOrderFilterAsync(
            input.NetworkCode,
            input.OrderId,
            input.AdUnitCode,
            input.ClientId,
            input.StartDate,
            input.EndDate,
            cancellationToken);
        return Aggregate(records, null, input.ClientId, input.StartDate, input.EndDate);
    }

    public async Task<List<OrderInfoDto>> GetOrdersAsync(GetOrdersQueryDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var pairs = await _dailyRepository.GetDistinctOrdersAsync(
            input.NetworkCode,
            input.AdUnitTopLevelCode,
            input.AdUnitCode,
            input.ClientId,
            cancellationToken);

        return pairs
            .Select(p => new OrderInfoDto { OrderId = p.OrderId, OrderName = p.OrderName })
            .ToList();
    }

    public async Task<GroupedBySubchannelResultDto> GetGroupedBySubchannelAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByCompositeFilterAsync(input.NetworkCode, input.AdUnitTopLevelCode, input.ClientId, input.DemandChannel, input.DemandSubchannelName, input.OrderId, input.OrderName, input.StartDate, input.EndDate, cancellationToken);

        var grouped = records
            .GroupBy(r => r.DemandSubchannelName ?? "Unknown")
            .Select(g => new GroupedBySubchannelItemDto
            {
                DemandSubchannelName = g.Key,
                TotalCodeServedCount = g.Sum(r => r.CodeServedCount),
                TotalImpressions = g.Sum(r => r.Impressions),
                TotalRevenue = g.Sum(r => r.Revenue)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        return new GroupedBySubchannelResultDto
        {
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            Items = grouped
        };
    }

        public async Task<GroupedByOrderResultDto> GetGroupedByOrderAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByCompositeFilterAsync(input.NetworkCode, input.AdUnitTopLevelCode, input.ClientId, input.DemandChannel, input.DemandSubchannelName, input.OrderId, input.OrderName, input.StartDate, input.EndDate, cancellationToken);

        var grouped = records
            .GroupBy(r => r.OrderName ?? "Unknown")
            .Select(g => new GroupedByOrderItemDto
            {
                OrderName = g.Key,
                TotalCodeServedCount = g.Sum(r => r.CodeServedCount),
                TotalImpressions = g.Sum(r => r.Impressions),
                TotalRevenue = g.Sum(r => r.Revenue),
                AverageEcpm = g.Sum(r => r.AverageEcpm * r.Impressions) / (g.Sum(r => r.Impressions) > 0 ? g.Sum(r => r.Impressions) : 1)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        return new GroupedByOrderResultDto
        {
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            Items = grouped
        };
    }

    public async Task<GroupedByDateResultDto> GetGroupedByDateAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EndDate < input.StartDate) throw new ArgumentException("EndDate must be >= StartDate");

        var records = await _dailyRepository.GetByCompositeFilterAsync(input.NetworkCode, input.AdUnitTopLevelCode, input.ClientId, input.DemandChannel, input.DemandSubchannelName, input.OrderId, input.OrderName, input.StartDate, input.EndDate, cancellationToken);

        var grouped = records
            .GroupBy(r => r.ReportDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                long impressions = g.Sum(r => r.Impressions);
                double weightedEcpm = g.Sum(r => r.AverageEcpm * r.Impressions);
                return new GroupedByDateItemDto
                {
                    ReportDate = g.Key,
                    TotalRevenue = g.Sum(r => r.Revenue),
                    TotalImpressions = impressions,
                    TotalCodeServedCount = g.Sum(r => r.CodeServedCount),
                    AverageEcpm = impressions > 0 ? weightedEcpm / impressions : 0d
                };
            })
            .ToList();

        long totalImpressions = grouped.Sum(x => x.TotalImpressions);
        double totalRevenue = grouped.Sum(x => x.TotalRevenue);
        long totalCodeServed = grouped.Sum(x => x.TotalCodeServedCount);
        double totalWeightedEcpm = records.Sum(r => r.AverageEcpm * r.Impressions);

        return new GroupedByDateResultDto
        {
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            TotalRevenue = totalRevenue,
            TotalImpressions = totalImpressions,
            TotalCodeServedCount = totalCodeServed,
            AverageEcpm = totalImpressions > 0 ? totalWeightedEcpm / totalImpressions : 0d,
            Items = grouped
        };
    }

    private static AggregatedReportResultDto Aggregate(
        List<DashboardResponse> records,
        string adUnitId,
        Guid? clientId,
        DateTime startDate,
        DateTime endDate,
        Boolean includeRecordBreakdown = false)
    {
        long totalCodeServed = 0, totalImpressions = 0, totalActiveView = 0;
        int sourceRowCount = 0;
        double totalRevenue = 0d;
        double weightedEcpmNumerator = 0d;

        var dailyItems = new List<DailyReportItemDto>(records.Count);

        foreach (var r in records)
        {
            totalCodeServed += r.CodeServedCount;
            totalImpressions += r.Impressions;
            totalActiveView += r.ActiveViewEligibleImpressions;
            totalRevenue += r.Revenue;
            weightedEcpmNumerator += r.AverageEcpm * r.Impressions;

            dailyItems.Add(new DailyReportItemDto
            {
                ReportDate = r.ReportDate,
                AdUnitCode = r.AdUnitClient?.AdUnitCode,
                ClientId = r.ClientId,
                DemandChannel = r.DemandChannel,
                DemandSubchannelName = r.DemandSubchannelName,
                OrderId = r.OrderId,
                OrderName = r.OrderName,
                CodeServedCount = r.CodeServedCount,
                Impressions = r.Impressions,
                Revenue = r.Revenue,
                ActiveViewEligibleImpressions = r.ActiveViewEligibleImpressions,
                AverageEcpm = r.AverageEcpm,
                SourceRowCount = ++sourceRowCount
            });
        }

        double averageEcpm = totalImpressions > 0 ? weightedEcpmNumerator / totalImpressions : 0d;

        return new AggregatedReportResultDto
        {
            AdUnitId = adUnitId,
            ClientId = clientId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalCodeServedCount = totalCodeServed,
            TotalImpressions = totalImpressions,
            TotalRevenue = totalRevenue,
            TotalActiveViewEligibleImpressions = totalActiveView,
            AverageEcpm = averageEcpm,
            RecordCount = dailyItems.Count,
            DailyBreakdown = includeRecordBreakdown ? dailyItems : null
        };
    }
}
