using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Consts;

namespace Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Submits;

public sealed class VideoRequestCreateDto
{
    [Required]
    [StringLength(VideoRequestConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    [NotDefault]
    public Guid? RefContentId { get; set; }

    [Required]
    [StringLength(VideoRequestConsts.RefContentTypeMaxLength)]
    public string? RefContentType { get; set; }

    [Required]
    [StringLength(VideoRequestConsts.StatusMaxLength)]
    public string? Status { get; set; }

    [Required]
    [StringLength(VideoRequestConsts.CurrentMilestoneMaxLength)]
    public string? CurrentMilestone { get; set; }

    [Required]
    [StringLength(VideoRequestConsts.MediaInputJsonMaxLength)]
    public string? MediaInputJson { get; set; }

    [StringLength(VideoRequestConsts.AudioProviderKeyMaxLength)]
    public string? AudioProviderKey { get; set; }

    [Required]
    [StringLength(VideoRequestConsts.VideoProviderKeyMaxLength)]
    public string? VideoProviderKey { get; set; }

    [StringLength(VideoRequestConsts.VideoProviderTrackingIdMaxLength)]
    public string? VideoProviderTrackingId { get; set; }

    [StringLength(VideoRequestConsts.VideoProviderUrlMaxLength)]
    public string? VideoProviderUrl { get; set; }

    [StringLength(VideoRequestConsts.VideoStorageUrlMaxLength)]
    public string? VideoStorageUrl { get; set; }

    [StringLength(VideoRequestConsts.VideoCdnUrlMaxLength)]
    public string? VideoCdnUrl { get; set; }

    [Required]
    public int RetryCount { get; set; }

    [StringLength(VideoRequestConsts.LastErrorMaxLength)]
    public string? LastError { get; set; }
}
