using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.ContentService.Domain.CustomerContentDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Submits;

/// <summary>What can be changed of a customer content.</summary>
public sealed class CustomerContentUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

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
