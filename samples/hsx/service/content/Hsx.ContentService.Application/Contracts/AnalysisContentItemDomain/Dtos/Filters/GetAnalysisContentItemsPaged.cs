using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Filters;

public sealed class GetAnalysisContentItemsPaged : PagedDataRequestDto
{
    /// <summary>A part of the scope key.</summary>
    [StringLength(100)]
    public string? ScopeKey { get; set; }

    /// <summary>Only the analysis content id.</summary>
    public Guid? AnalysisContentId { get; set; }

    /// <summary>Only the customer content id.</summary>
    public Guid? CustomerContentId { get; set; }
}
