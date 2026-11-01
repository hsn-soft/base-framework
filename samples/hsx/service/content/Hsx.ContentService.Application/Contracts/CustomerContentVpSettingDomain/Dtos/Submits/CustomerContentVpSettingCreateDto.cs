using System.ComponentModel.DataAnnotations;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Submits;

public sealed class CustomerContentVpSettingCreateDto
{
    [Required]
    [StringLength(CustomerContentVpSettingConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    [StringLength(CustomerContentVpSettingConsts.DomainNameMaxLength)]
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
