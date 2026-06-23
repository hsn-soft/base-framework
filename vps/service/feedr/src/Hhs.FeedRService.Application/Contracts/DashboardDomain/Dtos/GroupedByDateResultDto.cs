namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GroupedByDateItemDto
{
    public DateTime ReportDate { get; set; }
    public double TotalRevenue { get; set; }
    public long TotalImpressions { get; set; }
    public long TotalCodeServedCount { get; set; }
    public double AverageEcpm { get; set; }
}

public sealed class GroupedByDateResultDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double TotalRevenue { get; set; }
    public long TotalImpressions { get; set; }
    public long TotalCodeServedCount { get; set; }
    public double AverageEcpm { get; set; }
    public List<GroupedByDateItemDto> Items { get; set; } = new();
}
