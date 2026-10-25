using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Filters;

public sealed class GetUsersPaged : PagedDataRequestDto
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
