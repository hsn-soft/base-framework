using Hhs.FeedRService.EntityFrameworkCore;
using Hhs.FeedRService.MongoDb.Setup;
using HsnSoft.Base.Data;

namespace Hhs.FeedRService.AdManager;

/// <summary>
/// Delegates to both EF Core (PostgreSQL) and MongoDB seeders so that
/// the framework's single <see cref="IBasicDataSeeder"/> resolution
/// triggers both database initialization paths.
/// </summary>
public sealed class CompositeSeederService(
    EfCoreSeederService efCoreSeeder,
    MongoSeederService mongoSeeder) : IBasicDataSeeder
{
    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        // PostgreSQL first — ensures schema/tables exist before any service tries to write.
        await efCoreSeeder.EnsureSeedDataAsync(cancellationToken);
        await mongoSeeder.EnsureSeedDataAsync(cancellationToken);
    }
}
