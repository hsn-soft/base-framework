using System.ComponentModel.DataAnnotations;

namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GetReportByClientRangeDto
{
    [Required] public Guid ClientId { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
}
