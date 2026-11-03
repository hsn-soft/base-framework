using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Filters;

public sealed class GetPermissionsFilter : SortedAndLimitedDataRequestDto
{
    /// <summary>A part of the code.</summary>
    [StringLength(128)]
    public string? Code { get; set; }

    /// <summary><c>Page</c>, <c>Endpoint</c> or <c>Operation</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? Type { get; set; }
}
