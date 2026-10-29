using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Security;

namespace HsnSoft.Base.Authorization;

/// <summary>
/// Answers from the grants of the subjects of the current user (<see cref="ICurrentUser"/>: the user, then each of its roles) in the scope of the request (<see cref="IPermissionScopeProvider"/>, when the product has one). A grant to any of them
/// is a grant; there is no refusal. It works in an endpoint and in a message handler alike, as <see cref="ICurrentUser"/> does.
/// </summary>
public sealed class PermissionChecker(ICurrentUser user, IPermissionStore store, IEnumerable<IPermissionScopeProvider> scopeProviders) : IPermissionChecker
{
    public async Task<bool> IsGrantedAsync(string permission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        await foreach (PermissionGrants grants in GrantsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (grants.Has(permission))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<string?> GetValueAsync(string permission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        await foreach (PermissionGrants grants in GrantsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (grants.ValueOf(permission) is { } value)
            {
                return value;
            }
        }

        return null;
    }

    private async IAsyncEnumerable<PermissionGrants> GrantsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated)
        {
            yield break;
        }

        string? scope = scopeProviders.Select(provider => provider.GetScope()).FirstOrDefault(found => found is not null);
        if (user.Id is { } id)
        {
            yield return await store.GetAsync(scope, new PermissionSubject(PermissionProviders.User, id.ToString()), cancellationToken).ConfigureAwait(false);
        }

        foreach (string role in user.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return await store.GetAsync(scope, new PermissionSubject(PermissionProviders.Role, role), cancellationToken).ConfigureAwait(false);
        }
    }
}
