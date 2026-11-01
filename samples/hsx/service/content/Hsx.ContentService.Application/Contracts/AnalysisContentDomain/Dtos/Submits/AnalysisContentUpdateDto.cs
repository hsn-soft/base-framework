using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.ContentService.Domain.AnalysisContentDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Submits;

/// <summary>What can be changed of a analysis content.</summary>
public sealed class AnalysisContentUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [StringLength(AnalysisContentConsts.NormalizeStatusMaxLength)]
    public string? NormalizeStatus { get; set; }

    [StringLength(AnalysisContentConsts.VideoStatusMaxLength)]
    public string? VideoStatus { get; set; }

    [StringLength(AnalysisContentConsts.VideoCdnUrlMaxLength)]
    public string? VideoCdnUrl { get; set; }

    [StringLength(AnalysisContentConsts.CurrentMilestoneMaxLength)]
    public string? CurrentMilestone { get; set; }

    [StringLength(AnalysisContentConsts.LastErrorMaxLength)]
    public string? LastError { get; set; }
}
