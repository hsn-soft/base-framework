using System.ComponentModel.DataAnnotations;

namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

/// <summary>
/// DTO for filtering report data by Order (valid for all adUnitIds in the network).
/// An order criteria can be used with network and adUnit in API filtering.
/// </summary>
public sealed class GetReportByOrderFilterDto
{
    [Required] public string NetworkCode { get; set; }
    [Required] public string OrderId { get; set; }
    public string AdUnitCode { get; set; }
    public Guid? ClientId { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
}
