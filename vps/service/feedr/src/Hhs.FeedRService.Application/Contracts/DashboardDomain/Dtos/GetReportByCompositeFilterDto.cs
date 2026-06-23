using System.ComponentModel.DataAnnotations;

namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GetReportByCompositeFilterDto
{
    public string NetworkCode { get; set; }
    public string AdUnitTopLevelCode { get; set; }
    public string DemandChannel { get; set; }
    public string DemandSubchannelName { get; set; }
    public string OrderId { get; set; }
    public string OrderName { get; set; }
    public Guid? ClientId { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    public bool IncludeRecordBreakdown { get; set; } = true;
}
