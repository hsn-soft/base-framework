using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;

/// <summary>The roles of a user.</summary>
public sealed class AppUserRolesDto : IBaseDto
{
    public Guid UserId { get; init; }

    public List<Guid> RoleIds { get; init; } = [];
}
