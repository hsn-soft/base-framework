using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Filters;

public sealed class GetVideoRequestsPaged : PagedDataRequestDto
{
    /// <summary>A part of the scope key.</summary>
    [StringLength(100)]
    public string? ScopeKey { get; set; }

    /// <summary>Only the ref content id.</summary>
    public Guid? RefContentId { get; set; }

    /// <summary>A part of the ref content type.</summary>
    [StringLength(100)]
    public string? RefContentType { get; set; }

    /// <summary>A part of the status.</summary>
    [StringLength(100)]
    public string? Status { get; set; }
}
