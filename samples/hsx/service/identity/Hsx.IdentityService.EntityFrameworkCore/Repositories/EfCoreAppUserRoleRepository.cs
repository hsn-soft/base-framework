using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppUserRoleRepository(IdentityServiceDbContext db, IDataFilter dataFilter) : EfCoreRepository<IdentityServiceDbContext, AppUserRole, Guid>(db), IAppUserRoleRepository
{
    public async Task<IReadOnlyList<Guid>> GetUserIdsOfRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            IReadOnlyList<IdRow> ids = await GetListAsync(new ListQueryOptions<AppUserRole> { Filter = link => link.RoleId == roleId }, selector: link => new IdRow(link.UserId), cancellationToken).ConfigureAwait(false);
            return [.. ids.Select(row => row.Id)];
        }
    }
}
