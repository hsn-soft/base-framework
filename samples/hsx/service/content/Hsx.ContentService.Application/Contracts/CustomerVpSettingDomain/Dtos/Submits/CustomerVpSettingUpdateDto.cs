using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;

namespace Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Submits;

/// <summary>What can be changed of a customer vp setting.</summary>
public sealed class CustomerVpSettingUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    public bool IsBlocked { get; set; }

    [Required]
    public int DailyDirectVideoGenerationLimit { get; set; }

    [Required]
    public int DailyTrendVideoGenerationLimit { get; set; }

    [Required]
    public int DailyAnalysisVideoGenerationLimit { get; set; }

    [Required]
    public bool IsAnalysisSourcesEnabled { get; set; }
}
