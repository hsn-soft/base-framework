using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Filters;

public sealed class GetCustomerContentVisitsPaged : PagedDataRequestDto
{
    /// <summary>A part of the scope key.</summary>
    [StringLength(100)]
    public string? ScopeKey { get; set; }

    /// <summary>Only the customer content id.</summary>
    public Guid? CustomerContentId { get; set; }

    /// <summary>A part of the visit response.</summary>
    [StringLength(100)]
    public string? VisitResponse { get; set; }
}
