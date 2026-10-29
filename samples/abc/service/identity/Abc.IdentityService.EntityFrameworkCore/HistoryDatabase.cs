using Abc.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore;

public static class HistoryDatabase
{
    /// <summary>Creates the history database and its table when they are not there. Only the identity service does this; nothing is seeded.</summary>
    public static Task EnsureHistoryDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.EnsureDatabaseCreatedAsync<HistoryDbContext>();
    }
}
