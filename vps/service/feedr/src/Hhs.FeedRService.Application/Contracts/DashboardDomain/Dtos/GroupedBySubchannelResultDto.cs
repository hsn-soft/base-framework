namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GroupedBySubchannelItemDto
{
    public string DemandSubchannelName { get; set; }
    public long TotalCodeServedCount { get; set; }
    public long TotalImpressions { get; set; }
    public double TotalRevenue { get; set; }
}

public sealed class GroupedBySubchannelResultDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<GroupedBySubchannelItemDto> Items { get; set; } = new();
}
