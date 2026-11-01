using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Consts;

namespace Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Submits;

/// <summary>What can be changed of a customer content normalized request.</summary>
public sealed class CustomerContentNormalizedRequestUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

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
