using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Submits;

public sealed class AnalysisContentItemCreateDto
{
    [Required]
    [StringLength(AnalysisContentItemConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    [NotDefault]
    public Guid? AnalysisContentId { get; set; }

    [Required]
    [NotDefault]
    public Guid? CustomerContentId { get; set; }

    [Required]
    public int SortOrder { get; set; }

    [StringLength(AnalysisContentItemConsts.SourceTitleMaxLength)]
    public string? SourceTitle { get; set; }

    [StringLength(AnalysisContentItemConsts.SourceDescriptionMaxLength)]
    public string? SourceDescription { get; set; }

    [StringLength(AnalysisContentItemConsts.SourceImageUrlMaxLength)]
    public string? SourceImageUrl { get; set; }
}
