using Abc.IdentityService.Domain.UserDomain.Consts;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.IdentityService.EntityFrameworkCore;

public static class IdentityDatabase
{
    /// <summary>The user the seed writes as the author of the first users.</summary>
    public const string SeedAuthor = "system";

    /// <summary>
    /// Creates the database and its tables when they are not there and, when there is no user yet, adds two to sign in as: <c>admin</c> / <c>admin123</c> (roles admin and customer)
    /// and <c>alice</c> / <c>alice123</c> (customer). Only the identity service calls it: the database is the identity service's, the auth server only reads it (so it needs the identity service to have started once). These passwords are sample
    /// values, for a development database; a real system makes its first administrator some other way.
    /// </summary>
    public static async Task EnsureIdentityDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await services.EnsureDatabaseCreatedAsync<IdentityDbContext>().ConfigureAwait(false);

        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            if (await db.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            DateTimeOffset now = (scope.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow();
            db.Users.AddRange(
                User.Create("admin", "admin@abc.example", "Admin", "admin123", [UserRoles.Admin, UserRoles.Customer], now, SeedAuthor),
                User.Create("alice", "alice@abc.example", "Alice", "alice123", [UserRoles.Customer], now, SeedAuthor));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

}
