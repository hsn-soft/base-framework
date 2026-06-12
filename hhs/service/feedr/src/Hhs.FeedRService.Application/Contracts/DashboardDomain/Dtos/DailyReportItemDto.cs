namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

public sealed class DailyReportItemDto
{
    public DateTime ReportDate { get; set; }
    public string AdUnitCode { get; set; }
    public Guid ClientId { get; set; }
    public string DemandChannel { get; set; }
    public string DemandSubchannelName { get; set; }
    public string OrderId { get; set; }
    public string OrderName { get; set; }
    public long CodeServedCount { get; set; }
    public long Impressions { get; set; }
    public double Revenue { get; set; }
    public long ActiveViewEligibleImpressions { get; set; }
    public double AverageEcpm { get; set; }
    public int SourceRowCount { get; set; }
}
