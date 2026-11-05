using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;

public sealed class GetAppRolesPagedDto : PagedDataRequestDto
{
    /// <summary>A part of the name.</summary>
    [StringLength(100)]
    public string? Name { get; set; }
}
