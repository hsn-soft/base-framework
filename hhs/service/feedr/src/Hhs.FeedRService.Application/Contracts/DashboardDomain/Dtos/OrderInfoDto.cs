namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

/// <summary>
/// Represents a distinct Order found in the aggregated report data.
/// </summary>
public sealed class OrderInfoDto
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderName { get; set; } = string.Empty;
}
