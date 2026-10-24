using System.ComponentModel.DataAnnotations;
using Abc.IdentityService.Application.Contracts.Dtos;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Filters;

public sealed class GetUsersFilter : LimitedDataRequestDto
{
    /// <summary>A part of the user name.</summary>
    [StringLength(100)]
    public string? UserName { get; set; }

    /// <summary>A part of the email.</summary>
    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>Only the active (<see langword="true"/>) or only the inactive (<see langword="false"/>) users; all when it is not given.</summary>
    public bool? IsActive { get; set; }
}
