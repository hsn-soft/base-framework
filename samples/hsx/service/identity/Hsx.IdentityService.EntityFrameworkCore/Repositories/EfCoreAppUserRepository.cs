using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppUserRepository(IdentityServiceDbContext db, IDataFilter dataFilter) : EfCoreRepository<IdentityServiceDbContext, AppUser, Guid>(db), IAppUserRepository
{
    /// <summary>The lookup of the sign-in. Nobody is signed in, so the filter of the tenants has nothing to compare with and is off; the tenant and the normalized email or user name name the user, and a deleted user does not sign in.</summary>
    public Task<SignInInfo?> FindByEmailAsync(Guid tenantId, string normalizedEmail, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(normalizedEmail);
        return FindSignInAsync(user => user.TenantId == tenantId && user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<SignInInfo?> FindByUserNameAsync(Guid tenantId, string normalizedUserName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(normalizedUserName);
        return FindSignInAsync(user => user.TenantId == tenantId && user.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public Task<SignInInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => FindSignInAsync(user => user.Id == userId, cancellationToken);

    public async Task RecordFailureAsync(Guid userId, int maxFailedLoginCount, int lockoutMinutes, CancellationToken cancellationToken = default)
    {
        DateTimeOffset lockedUntil = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
        using (dataFilter.Disable<IMultiTenant>())
        {
            // one command: of two failures at once both are counted
            await UpdateWhereAsync(
                user => user.Id == userId,
                set => set
                    .SetProperty(user => user.FailedLoginCount, user => user.FailedLoginCount + 1 >= maxFailedLoginCount ? 0 : user.FailedLoginCount + 1)
                    .SetProperty(user => user.LockoutEndAt, user => user.FailedLoginCount + 1 >= maxFailedLoginCount ? lockedUntil : user.LockoutEndAt),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RecordSuccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using (dataFilter.Disable<IMultiTenant>())
        {
            await UpdateWhereAsync(
                user => user.Id == userId,
                set => set.SetProperty(user => user.FailedLoginCount, 0).SetProperty(user => user.LockoutEndAt, (DateTimeOffset?)null).SetProperty(user => user.LastLoginAt, now),
                cancellationToken).ConfigureAwait(false);
        }
    }

    // a deleted user keeps its email (the index counts it), so the filters are off for this check
    public Task<bool> EmailExistsAsync(Guid tenantId, string normalizedEmail, Guid? exceptId = null, CancellationToken cancellationToken = default)
        => AnyAsync(user => user.TenantId == tenantId && user.NormalizedEmail == normalizedEmail && (exceptId == null || user.Id != exceptId), ignoreFilters: true, cancellationToken);

    public Task<bool> UserNameExistsAsync(Guid tenantId, string normalizedUserName, Guid? exceptId = null, CancellationToken cancellationToken = default)
        => AnyAsync(user => user.TenantId == tenantId && user.NormalizedUserName == normalizedUserName && (exceptId == null || user.Id != exceptId), ignoreFilters: true, cancellationToken);

    private async Task<SignInInfo?> FindSignInAsync(System.Linq.Expressions.Expression<Func<AppUser, bool>> filter, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppUser> found;
        using (dataFilter.Disable<IMultiTenant>())
        {
            found = await GetListAsync(
                new ListQueryOptions<AppUser> { Filter = filter, MaxResultCount = 1 },
                include: query => query.Include(user => user.Tenant).Include(user => user.UserRoles).ThenInclude(link => link.Role).ThenInclude(role => role!.Subscriptions).ThenInclude(link => link.Subscription),
                cancellationToken).ConfigureAwait(false);
        }

        if (found.Count == 0)
        {
            return null;
        }

        AppUser user = found[0];
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<AppRole> roles = [.. user.UserRoles.Select(link => link.Role).OfType<AppRole>()];
        List<string> scopeKeys =
        [
            .. roles.SelectMany(role => role.Subscriptions)
                .Where(link => !link.IsBlocked && link.Subscription is { EndedAt: null, IsBlocked: false } subscription && (subscription.ValidTo == null || subscription.ValidTo > now))
                .Select(link => ScopeKeyHelper.Generate(link.Subscription!.CustomerId, link.Subscription.ProductType))
                .Distinct(StringComparer.Ordinal),
        ];
        return new SignInInfo(user, user.Tenant ?? throw TenantNotFound.For(user.TenantId), [.. roles.Select(role => role.Name).Distinct(StringComparer.Ordinal)], scopeKeys);
    }
}
