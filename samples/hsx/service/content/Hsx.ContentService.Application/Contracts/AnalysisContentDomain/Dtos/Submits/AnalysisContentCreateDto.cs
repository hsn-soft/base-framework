using System.ComponentModel.DataAnnotations;
using Hsx.ContentService.Domain.AnalysisContentDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Submits;

public sealed class AnalysisContentCreateDto
{
    [Required]
    [StringLength(AnalysisContentConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    public DateTimeOffset? AnalysisDate { get; set; }

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
