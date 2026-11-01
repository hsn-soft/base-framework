using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Filters;

public sealed class GetCustomerContentNormalizedRequestsPaged : PagedDataRequestDto
{
    /// <summary>A part of the scope key.</summary>
    [StringLength(100)]
    public string? ScopeKey { get; set; }

    /// <summary>Only the customer content id.</summary>
    public Guid? CustomerContentId { get; set; }

    /// <summary>A part of the status.</summary>
    [StringLength(100)]
    public string? Status { get; set; }

    /// <summary>A part of the scraping status.</summary>
    [StringLength(100)]
    public string? ScrapingStatus { get; set; }

    /// <summary>A part of the outline status.</summary>
    [StringLength(100)]
    public string? OutlineStatus { get; set; }
}
