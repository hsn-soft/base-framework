using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Edx.Shared.Helper;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Edx.IdentityService.EntityFrameworkCore;

public static class IdentityDatabase
{
    private static readonly (Guid Id, string Code, string Name)[] s_tenants =
    [
        (EdxTenants.System, EdxTenants.SystemCode, "System"),
        (EdxTenants.Veriban, EdxTenants.VeribanCode, "Veriban"),
        (EdxTenants.Bilgera, EdxTenants.BilgeraCode, "Bilgera"),
    ];

    /// <summary>
    /// Creates the database and its tables when they are not there and, when there is no tenant yet, makes the three tenants (<c>system-tenant</c>, <c>veriban</c>, <c>bilgera</c>, with the ids of <c>EdxTenants</c>) and
    /// the users the <c>Seed</c> section names, each with a role of its own name in its tenant: the seed acts as each tenant in turn, so every row gets the tenant it belongs to. Only the identity service does this: the
    /// database is the identity service's, the auth server only reads it (so it needs the identity service to have started once). The passwords are the configuration's, sample values for a development database.
    /// </summary>
    public static async Task EnsureIdentityDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await services.EnsureDatabaseCreatedAsync<IdentityDbContext>().ConfigureAwait(false);

        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            if (await db.Tenants.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            db.Tenants.AddRange(s_tenants.Select(tenant => Tenant.Create(tenant.Id, tenant.Code, tenant.Name)));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            ICurrentTenant currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            IReadOnlyList<SeedUser> seed = scope.ServiceProvider.GetRequiredService<IOptions<IdentitySeedOptions>>().Value.Users;
            foreach ((Guid id, string code, _) in s_tenants)
            {
                using (currentTenant.Change(id, isSystemTenant: id == EdxTenants.System))
                {
                    foreach (SeedUser seedUser in seed.Where(user => user.Tenant.Equals(code, StringComparison.OrdinalIgnoreCase)))
                    {
                        Role role = Role.Create(seedUser.UserName);
                        User user = User.Create(seedUser.UserName, $"{seedUser.UserName}@{code}.example", $"{seedUser.UserName} ({code})", seedUser.Password);
                        db.Roles.Add(role);
                        db.Users.Add(user);
                        db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
                    }

                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
