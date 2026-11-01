using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreTenantRepository(IdentityDbContext db) : ITenantRepository
{
    public async Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        string path = tenant.AccessPath;
        return await db.Tenants.AsNoTracking()
            .Where(candidate => candidate.AccessPath.StartsWith(path) && candidate.Id != tenant.Id && candidate.IsActive)
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveAsync(Tenant tenant, Tenant? newParent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        string oldPath = tenant.AccessPath;
        if (newParent is not null && TenantAccessPath.IsSelfOrDescendantOf(newParent.AccessPath, oldPath))
        {
            throw new InvalidOperationException($"The tenant '{tenant.Code}' cannot be moved under '{newParent.Code}': it is the tenant itself or under it.");
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
