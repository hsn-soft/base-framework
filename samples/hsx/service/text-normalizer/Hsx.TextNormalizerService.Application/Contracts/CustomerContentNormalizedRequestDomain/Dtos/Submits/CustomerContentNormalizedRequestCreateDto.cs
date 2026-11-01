using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Consts;

namespace Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Submits;

public sealed class CustomerContentNormalizedRequestCreateDto
{
    [Required]
    [StringLength(CustomerContentNormalizedRequestConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    [NotDefault]
    public Guid? CustomerContentId { get; set; }

    [Required]
    [StringLength(CustomerContentNormalizedRequestConsts.DomainNameMaxLength)]
    public string? DomainName { get; set; }

    [Required]
    [StringLength(CustomerContentNormalizedRequestConsts.ContentKeyMaxLength)]
    public string? ContentKey { get; set; }

    [StringLength(CustomerContentNormalizedRequestConsts.CorrelationIdMaxLength)]
    public string? CorrelationId { get; set; }

    [Required]
    [StringLength(CustomerContentNormalizedRequestConsts.StatusMaxLength)]
    public string? Status { get; set; }

    [Required]
    [StringLength(CustomerContentNormalizedRequestConsts.CurrentMilestoneMaxLength)]
    public string? CurrentMilestone { get; set; }

    [StringLength(CustomerContentNormalizedRequestConsts.ScrapingStatusMaxLength)]
    public string? ScrapingStatus { get; set; }

    [StringLength(CustomerContentNormalizedRequestConsts.OutlineStatusMaxLength)]
    public string? OutlineStatus { get; set; }

    [Required]
    public int RetryCount { get; set; }

    [StringLength(CustomerContentNormalizedRequestConsts.LastErrorMaxLength)]
    public string? LastError { get; set; }
}
