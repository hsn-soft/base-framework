using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreTenantRepository(IdentityDbContext db) : ITenantRepository
{
    public async Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        string path = tenant.AccessPath;
        Guid id = tenant.Id;
        return await db.Tenants.AsNoTracking()
            .Where(candidate => candidate.AccessPath.StartsWith(path) && candidate.Id != id && candidate.IsActive)
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> HasChildrenAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => db.Tenants.AnyAsync(candidate => candidate.ParentId == tenantId, cancellationToken);

    public async Task MoveAsync(Tenant tenant, Tenant? newParent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        string oldPath = tenant.AccessPath;
        if (newParent is not null && TenantAccessPath.IsSelfOrDescendantOf(newParent.AccessPath, oldPath))
        {
            throw new TenantLoopException();
        }

        List<Tenant> moving = await db.Tenants.Where(candidate => candidate.AccessPath.StartsWith(oldPath)).ToListAsync(cancellationToken).ConfigureAwait(false);
        tenant.PlaceUnder(newParent);
        string newPath = tenant.AccessPath;
        foreach (Tenant under in moving.Where(candidate => candidate.Id != tenant.Id))
        {
            under.SetAccessPath(TenantAccessPath.Move(under.AccessPath, oldPath, newPath));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
