using Abc.IdentityService.Domain.UserDomain.Consts;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore;

/// <summary>The tenants the users of the sample belong to: the system tenant, whose users see every tenant, and the company of <c>alice</c>.</summary>
internal sealed record IdentitySeedTenants(Guid SystemTenantId, Guid CompanyTenantId);

/// <summary>
/// Seeds the identity database (registered by <see cref="EfCoreServiceCollectionExtensions.AddIdentityPersistence"/>, after the step that makes its schema): when there is no user yet, adds two to sign in as:
/// <c>admin</c> / <c>admin123</c> (roles admin and customer, in English, in the system tenant) and <c>alice</c> / <c>alice123</c> (customer, in Turkish, in the company tenant). Only the host that owns the database does this
/// (<c>SeedStartupConnections</c>): the auth server only reads it; an environment that must not have them lists <c>IdentityUsersSeeder</c> in <c>Seeding:Disabled</c>. These passwords are sample values, for a development
/// database; a real system makes its first administrator some other way.
/// </summary>
internal sealed class IdentityUsersSeeder(IdentityDbContext db, ICurrentTenant tenant, IdentitySeedTenants tenants) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // a user is made in the tenant that is current: the seed acts as each tenant in turn
        using (tenant.Change(tenants.SystemTenantId, isSystemTenant: true))
        {
            db.Users.Add(User.Create("admin", "admin@abc.example", "Admin", "admin123", [UserRoles.Admin, UserRoles.Customer], "en"));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        using (tenant.Change(tenants.CompanyTenantId))
        {
            db.Users.Add(User.Create("alice", "alice@abc.example", "Alice", "alice123", [UserRoles.Customer], "tr"));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
