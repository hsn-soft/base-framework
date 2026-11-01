using System.ComponentModel.DataAnnotations;
using Hsx.ContentService.Domain.CustomerVpSettingDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Submits;

public sealed class CustomerVpSettingCreateDto
{
    [Required]
    [StringLength(CustomerVpSettingConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    [StringLength(CustomerVpSettingConsts.DomainNameMaxLength)]
    public string? DomainName { get; set; }

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
