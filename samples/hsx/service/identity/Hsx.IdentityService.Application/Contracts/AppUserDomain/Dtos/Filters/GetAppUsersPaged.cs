using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;

public sealed class GetAppUsersPaged : PagedDataRequestDto
{
    /// <summary>A part of the user name.</summary>
    [StringLength(100)]
    public string? UserName { get; set; }

    /// <summary>A part of the email.</summary>
    [StringLength(100)]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }
}
