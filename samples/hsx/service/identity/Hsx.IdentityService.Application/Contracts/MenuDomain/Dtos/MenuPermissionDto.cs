using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Application.Contracts.MenuDomain.Dtos;

/// <summary>A permission of the page of a menu item: what it is to the page and whether the caller has it.</summary>
public sealed class MenuPermissionDto : IBaseDto
{
    public string Code { get; init; } = string.Empty;

    public PermissionType Type { get; init; }

    /// <summary><c>Visibility</c> (the page opens with it), <c>Required</c> (what it reads with), <c>Action</c> (a button), <c>Operation</c> (a field) or <c>Constraint</c> (a limit).</summary>
    public MenuPermissionRelationType Relation { get; init; }

    public bool Granted { get; init; }
}
