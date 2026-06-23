namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

public sealed class AggregatedReportResultDto
{
    public string AdUnitId { get; set; }
    public Guid? ClientId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Sum totals across the range
    public long TotalCodeServedCount { get; set; }
    public long TotalImpressions { get; set; }
    public double TotalRevenue { get; set; }
    public long TotalActiveViewEligibleImpressions { get; set; }

    // Impressions-weighted average across the range
    public double AverageEcpm { get; set; }

    public int RecordCount { get; set; }
    public List<DailyReportItemDto> DailyBreakdown { get; set; } = new();
}
