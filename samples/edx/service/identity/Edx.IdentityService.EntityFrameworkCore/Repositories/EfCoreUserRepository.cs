using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Text;
using Microsoft.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>The users in SQL Server: what <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> does for any entity, the roles of users, and the lookup the auth server signs in with.</summary>
public sealed class EfCoreUserRepository(IdentityDbContext db) : EfCoreRepository<IdentityDbContext, User, Guid>(db), IUserRepository
{
    public async Task<SignInInfo?> FindForSignInAsync(string tenantCode, string userName, CancellationToken cancellationToken = default)
    {
        string code = StringHelper.Normalize(tenantCode).ToLowerInvariant();
        Tenant? tenant = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return null;
        }

        // nobody is signed in yet, so the tenant filter has no tenant to filter by: the tenant the caller named is looked in, by its id, on purpose
        string name = StringHelper.Normalize(userName);
        User? user = await Db.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.TenantId == tenant.Id && entity.NormalizedUserName == name, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        List<string> roles = await Db.UserRoles.IgnoreQueryFilters().AsNoTracking()
            .Where(link => link.TenantId == tenant.Id && link.UserId == user.Id)
            .Join(Db.Roles.IgnoreQueryFilters(), link => link.RoleId, role => role.Id, (_, role) => role.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new SignInInfo(user, tenant, roles);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRoleNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        // the filters of the context apply: the links and the roles read are the caller's tenant's
        var pairs = await Db.UserRoles.AsNoTracking()
            .Where(link => userIds.Contains(link.UserId))
            .Join(Db.Roles, link => link.RoleId, role => role.Id, (link, role) => new { link.UserId, role.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return pairs.GroupBy(pair => pair.UserId).ToDictionary(group => group.Key, group => (IReadOnlyList<string>)[.. group.Select(pair => pair.Name).Order()]);
    }
}
