using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreTenantRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, Tenant, Guid>(db), ITenantRepository
{
    public async Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        string path = tenant.NormalizedAccessPath;
        Guid id = tenant.Id;
        IReadOnlyList<IdRow> ids = await GetListAsync(
            new ListQueryOptions<Tenant> { Filter = candidate => candidate.NormalizedAccessPath.StartsWith(path) && candidate.Id != id && candidate.IsActive },
            selector: candidate => new IdRow(candidate.Id),
            cancellationToken).ConfigureAwait(false);
        return [.. ids.Select(row => row.Id)];
    }

    public Task<bool> HasChildrenAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => AnyAsync(candidate => candidate.ParentId == tenantId, cancellationToken: cancellationToken);

    public async Task MoveAsync(Tenant tenant, Tenant? newParent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        string oldPath = tenant.NormalizedAccessPath;
        if (newParent is not null && TenantAccessPath.IsSelfOrDescendantOf(newParent.NormalizedAccessPath, oldPath))
        {
            throw new TenantLoopException();
        }

        tenant.PlaceUnder(newParent);
        string newPath = tenant.NormalizedAccessPath;
        Guid id = tenant.Id;
        Guid? parentId = newParent?.Id;

        // an expression that goes to the database cannot use the span form of Substring
#pragma warning disable CA1845
        // one command: the tenant gets its parent and every path under the old one gets the new start, together
        await UpdateWhereAsync(
            candidate => candidate.NormalizedAccessPath.StartsWith(oldPath),
            set => set
                .SetProperty(candidate => candidate.NormalizedAccessPath, candidate => newPath + candidate.NormalizedAccessPath.Substring(oldPath.Length))
                .SetProperty(candidate => candidate.ParentId, candidate => candidate.Id == id ? parentId : candidate.ParentId),
            cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1845
    }
}
