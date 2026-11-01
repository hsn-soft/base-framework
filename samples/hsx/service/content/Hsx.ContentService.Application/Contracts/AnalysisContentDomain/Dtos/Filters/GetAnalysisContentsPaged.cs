using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Filters;

public sealed class GetAnalysisContentsPaged : PagedDataRequestDto
{
    /// <summary>A part of the scope key.</summary>
    [StringLength(100)]
    public string? ScopeKey { get; set; }

    /// <summary>A part of the normalize status.</summary>
    [StringLength(100)]
    public string? NormalizeStatus { get; set; }

    /// <summary>A part of the video status.</summary>
    [StringLength(100)]
    public string? VideoStatus { get; set; }
}
