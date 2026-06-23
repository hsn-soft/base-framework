namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GroupedByOrderItemDto
{
    public string OrderName { get; set; }
    public long TotalCodeServedCount { get; set; }
    public long TotalImpressions { get; set; }
    public double TotalRevenue { get; set; }
    public double AverageEcpm { get; set; }
}

public sealed class GroupedByOrderResultDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<GroupedByOrderItemDto> Items { get; set; } = new();
}
