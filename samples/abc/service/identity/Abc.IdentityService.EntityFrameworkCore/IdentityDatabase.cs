using Abc.IdentityService.Domain.UserDomain.Consts;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.IdentityService.EntityFrameworkCore;

public static class IdentityDatabase
{
    /// <summary>
    /// Creates the database and its tables when they are not there and, when there is no user yet, adds two to sign in as: <c>admin</c> / <c>admin123</c> (roles admin and customer, in English, in
    /// <paramref name="systemTenantId"/>, whose users see every tenant) and <c>alice</c> / <c>alice123</c> (customer, in Turkish, in <paramref name="companyTenantId"/>). Only the identity service does this: the
    /// database is the identity service's, the auth server only reads it (so it needs the identity service to have started once). These passwords are sample values, for a development
    /// database; a real system makes its first administrator some other way.
    /// </summary>
    public static async Task EnsureIdentityDatabaseAsync(this IServiceProvider services, Guid systemTenantId, Guid companyTenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await services.EnsureDatabaseCreatedAsync<IdentityDbContext>().ConfigureAwait(false);

        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            if (await db.Users.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            // a user is made in the tenant that is current: the seed acts as each tenant in turn
            ICurrentTenant tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            using (tenant.Change(systemTenantId, isSystemTenant: true))
            {
                db.Users.Add(User.Create("admin", "admin@abc.example", "Admin", "admin123", [UserRoles.Admin, UserRoles.Customer], "en"));
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            using (tenant.Change(companyTenantId))
            {
                db.Users.Add(User.Create("alice", "alice@abc.example", "Alice", "alice123", [UserRoles.Customer], "tr"));
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
