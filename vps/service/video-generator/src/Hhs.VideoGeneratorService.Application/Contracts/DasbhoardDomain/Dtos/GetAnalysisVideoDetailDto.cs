using System.ComponentModel.DataAnnotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GetAnalysisVideoDetailRequest
{
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
}

