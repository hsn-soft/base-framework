using System.ComponentModel.DataAnnotations;

namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

/// <summary>
/// DTO for filtering report data by DemandChannel and DemandSubchannelName (independent attributes eligible for all networks/adUnits).
/// </summary>
public sealed class GetReportByDemandChannelFilterDto
{
    [Required] public string NetworkCode { get; set; }
    [Required] public string AdUnitTopLevelCode { get; set; }
    [Required] public string DemandChannel { get; set; }
    [Required] public string DemandSubchannelName { get; set; }
    public Guid? ClientId { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
}
