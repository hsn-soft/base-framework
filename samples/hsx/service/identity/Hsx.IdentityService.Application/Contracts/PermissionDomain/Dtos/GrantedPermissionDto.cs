using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;

/// <summary>A permission that was granted, with the value when it is a constraint (<c>constraint.audit-logs.max-days</c>: <c>30</c>) and nothing for any other.</summary>
public sealed class GrantedPermissionDto : IBaseDto
{
    public string Code { get; init; } = string.Empty;

    public PermissionType Type { get; init; }

    public string? Value { get; init; }
}
