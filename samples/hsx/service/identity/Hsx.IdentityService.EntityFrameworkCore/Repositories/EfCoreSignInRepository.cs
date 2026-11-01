using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>
/// The lookup of the sign-in. Nobody is signed in, so the tenant filter has nothing to compare with: the query ignores the filters and the email names the user. The roles and the scope keys are the ones of that user's tenant.
/// </summary>
internal sealed class EfCoreSignInRepository(IdentityDbContext db) : ISignInRepository
{
    public async Task<SignInInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = StringHelper.Normalize(email);
        AppUser? user = await db.AppUsers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == normalized, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        Tenant tenant = await db.Tenants.AsNoTracking().FirstAsync(candidate => candidate.Id == user.TenantId, cancellationToken).ConfigureAwait(false);
        Guid userId = user.Id;
        List<Guid> roleIds = await db.AppUserRoles.IgnoreQueryFilters().AsNoTracking().Where(link => link.UserId == userId).Select(link => link.RoleId).ToListAsync(cancellationToken).ConfigureAwait(false);
        List<string> roles = await db.AppRoles.IgnoreQueryFilters().AsNoTracking().Where(role => roleIds.Contains(role.Id)).Select(role => role.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        List<string> scopeKeys = await (
            from link in db.AppRoleSubscriptions.IgnoreQueryFilters().AsNoTracking()
            join subscription in db.Subscriptions.AsNoTracking() on link.SubscriptionId equals subscription.Id
            where roleIds.Contains(link.RoleId) && !subscription.IsBlocked && (subscription.ValidTo == null || subscription.ValidTo > DateTimeOffset.UtcNow)
            select subscription.ScopeKey).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false);
        return new SignInInfo(user, tenant, roles, scopeKeys);
    }
}
