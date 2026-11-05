using System.ComponentModel.DataAnnotations;
using Hsx.IdentityService.Domain.PermissionDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;

/// <summary>A permission to grant, with the value when it is a constraint and nothing for any other.</summary>
public sealed class GrantInputDto
{
    [Required]
    [StringLength(PermissionConsts.CodeMaxLength)]
    public string? Code { get; set; }

    [StringLength(PermissionConsts.ValueMaxLength)]
    public string? Value { get; set; }
}
