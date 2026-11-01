using System.ComponentModel.DataAnnotations;
using Hsx.ContentService.Domain.CustomerContentDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Submits;

public sealed class CustomerContentCreateDto
{
    [Required]
    [StringLength(CustomerContentConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    [StringLength(CustomerContentConsts.ContentKeyMaxLength)]
    public string? ContentKey { get; set; }

    [Required]
    [StringLength(CustomerContentConsts.SlugKeyMaxLength)]
    public string? SlugKey { get; set; }

    [StringLength(CustomerContentConsts.NormalizeStatusMaxLength)]
    public string? NormalizeStatus { get; set; }

    [StringLength(CustomerContentConsts.VideoStatusMaxLength)]
    public string? VideoStatus { get; set; }

    [StringLength(CustomerContentConsts.VideoCdnUrlMaxLength)]
    public string? VideoCdnUrl { get; set; }

    [StringLength(CustomerContentConsts.CurrentMilestoneMaxLength)]
    public string? CurrentMilestone { get; set; }

    [StringLength(CustomerContentConsts.LastErrorMaxLength)]
    public string? LastError { get; set; }
}
