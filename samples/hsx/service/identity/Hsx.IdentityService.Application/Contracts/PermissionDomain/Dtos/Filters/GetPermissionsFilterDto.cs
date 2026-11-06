using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Filters;

public sealed class GetPermissionsFilterDto : SortedAndLimitedDataRequestDto
{
    /// <summary>A part of the code.</summary>
    [StringLength(128)]
    public string? Code { get; set; }

    /// <summary>Only the permissions of this type; all when it is not given.</summary>
    public PermissionType? Type { get; set; }
}
