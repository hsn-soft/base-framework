using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Submits;

/// <summary>What can be changed of a analysis content item.</summary>
public sealed class AnalysisContentItemUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    public int SortOrder { get; set; }

    [StringLength(AnalysisContentItemConsts.SourceTitleMaxLength)]
    public string? SourceTitle { get; set; }

    [StringLength(AnalysisContentItemConsts.SourceDescriptionMaxLength)]
    public string? SourceDescription { get; set; }

    [StringLength(AnalysisContentItemConsts.SourceImageUrlMaxLength)]
    public string? SourceImageUrl { get; set; }
}
