using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Edx.Shared.Helper;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.IdentityService.EntityFrameworkCore;

public static class IdentityDatabase
{
    internal static readonly (Guid Id, string Code, string Name)[] s_tenants =
    [
        (EdxTenants.System, EdxTenants.SystemCode, "System"),
        (EdxTenants.Veriban, EdxTenants.VeribanCode, "Veriban"),
        (EdxTenants.Bilgera, EdxTenants.BilgeraCode, "Bilgera"),
    ];

    /// <summary>
    /// Creates the database and its tables when they are not there and makes the tenants that are missing (<c>system-tenant</c>, <c>veriban</c>, <c>bilgera</c>, with the ids of <c>EdxTenants</c>). The roles and the users of the sample are made by
    /// <see cref="SampleIdentity.EnsureSampleIdentityAsync"/>, after the permissions (a role is given its starting grants when it is made). Only the identity service does this: the database is the identity service's, the auth server only reads it (so it needs the identity
    /// service to have started once).
    /// </summary>
    public static async Task EnsureIdentityDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await services.EnsureDatabaseCreatedAsync<IdentityDbContext>().ConfigureAwait(false);

        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            HashSet<Guid> known = [.. await db.Tenants.Select(tenant => tenant.Id).ToListAsync(cancellationToken).ConfigureAwait(false)];
            db.Tenants.AddRange(s_tenants.Where(tenant => !known.Contains(tenant.Id)).Select(tenant => Tenant.Create(tenant.Id, tenant.Code, tenant.Name)));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
