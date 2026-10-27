using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;

public sealed class GetUsersPaged : PagedDataRequestDto
{
    /// <summary>A part of the user name.</summary>
    [StringLength(64)]
    public string? UserName { get; set; }

    /// <summary>A part of the email.</summary>
    [StringLength(256)]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }
}

public sealed class GetRolesFilter : SortedAndLimitedDataRequestDto
{
    /// <summary>A part of the name.</summary>
    [StringLength(128)]
    public string? Name { get; set; }
}

public sealed class GetTenantsFilter : SortedAndLimitedDataRequestDto
{
    /// <summary>A part of the code.</summary>
    [StringLength(128)]
    public string? SearchText { get; set; }
}

public sealed class GetPermissionsFilter : SortedAndLimitedDataRequestDto
{
    /// <summary>A part of the code.</summary>
    [StringLength(300)]
    public string? Code { get; set; }

    /// <summary><c>Endpoint</c>, <c>Operation</c> or <c>Constraint</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? Type { get; set; }
}
