using System;
using System.Collections.Generic;
using System.Threading;

namespace HsnSoft.Base.Security;

/// <summary>Who an <see cref="ActingUser"/> scope acts as: the user and the tenant of whatever runs inside it.</summary>
/// <param name="Id">The id of the user (what the audit fields keep).</param>
/// <param name="UserName">The user name.</param>
/// <param name="Roles">The roles of the user.</param>
/// <param name="TenantId">The tenant of the user.</param>
/// <param name="IsSystemTenant">Whether the user belongs to the system tenant, which sees every tenant.</param>
public sealed record UserContext(Guid? Id, string? UserName, IReadOnlyList<string> Roles, Guid? TenantId, bool IsSystemTenant);

/// <summary>
/// The user that code runs as when there is no request that says who it is: a message handler (the user who started the chain), a worker that acts for a user, a seed. <see cref="ICurrentUser"/> answers from it when there is no
/// signed-in user on a request, and the tenant of <see cref="MultiTenancy.ICurrentTenant"/> follows, so the audit fields and the query filters work in a handler as they do in a controller. It follows the asynchronous calls and is
/// not seen by other flows.
/// </summary>
public static class ActingUser
{
    private static readonly AsyncLocal<UserContext?> s_current = new();

    /// <summary>The user the code acts as now, or <see langword="null"/> when no scope is open.</summary>
    public static UserContext? Current => s_current.Value;

    /// <summary>Acts as <paramref name="user"/> until the returned scope is disposed (<see langword="null"/> acts as nobody); the previous user comes back.</summary>
    public static IDisposable Use(UserContext? user)
    {
        UserContext? previous = s_current.Value;
        s_current.Value = user;
        return new Restore(previous);
    }

    private sealed class Restore(UserContext? previous) : IDisposable
    {
        public void Dispose() => s_current.Value = previous;
    }
}
