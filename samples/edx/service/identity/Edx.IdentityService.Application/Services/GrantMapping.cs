using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Exceptions;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;

namespace Edx.IdentityService.Application.Services;

/// <summary>What the two services that set grants (of a role, of a user) share: the grants a client asked for turned into permissions of the catalogue (a code that is not there, or not active, is refused), and a grant shown to the client.</summary>
internal static class GrantMapping
{
    public static async Task<List<(Permission Permission, string? Value)>> ResolveAsync(IPermissionGrantRepository grants, SetGrantsDto input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        // a code given twice is one grant (the last value wins)
        Dictionary<string, string?> asked = [];
        foreach (GrantInputDto grant in input.Grants)
        {
            asked[grant.Code.Trim().ToLowerInvariant()] = grant.Value;
        }

        IReadOnlyList<Permission> found = await grants.FindActiveByCodesAsync(asked.Keys, cancellationToken).ConfigureAwait(false);
        Dictionary<string, Permission> byCode = found.ToDictionary(permission => permission.Code);
        // a constraint has to have its value and any other permission must have none: said here, so a grant that is wrong stops the change before anything is written
        return [.. asked.Select(item =>
        {
            Permission permission = byCode.GetValueOrDefault(item.Key) ?? throw PermissionNotFound.For(item.Key);
            return (permission, permission.CheckValue(item.Value));
        })];
    }

    public static List<GrantedPermissionDto> ToDtos(IReadOnlyList<GrantedPermission> granted)
        => [.. granted.Select(item => new GrantedPermissionDto { Code = item.Code, Type = item.Type.ToString(), Value = item.Value })];
}
