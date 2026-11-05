using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppRoleSubscriptionRepository(IdentityServiceDbContext db, IDataFilter dataFilter) : EfCoreRepository<IdentityServiceDbContext, AppRoleSubscription, Guid>(db), IAppRoleSubscriptionRepository
{
    public async Task<IReadOnlyList<Guid>> GetSubscriptionIdsOfRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            IReadOnlyList<IdRow> ids = await GetListAsync(new ListQueryOptions<AppRoleSubscription> { Filter = link => link.RoleId == roleId }, selector: link => new IdRow(link.SubscriptionId), cancellationToken).ConfigureAwait(false);
            return [.. ids.Select(row => row.Id)];
        }
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsOfSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            IReadOnlyList<List<Guid>> users = await GetListAsync(
                new ListQueryOptions<AppRoleSubscription> { Filter = link => link.SubscriptionId == subscriptionId },
                selector: link => link.Role!.UserRoles.Select(userRole => userRole.UserId).ToList(),
                cancellationToken).ConfigureAwait(false);
            return [.. users.SelectMany(ids => ids).Distinct()];
        }
    }

    public async Task DeleteForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            await DeleteWhereAsync(link => link.SubscriptionId == subscriptionId, cancellationToken).ConfigureAwait(false);
        }
    }
}
