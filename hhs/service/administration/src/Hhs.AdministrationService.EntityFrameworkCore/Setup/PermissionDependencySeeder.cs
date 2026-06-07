using Hhs.AdministrationService.EntityFrameworkCore.Context;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.AdministrationService.EntityFrameworkCore.Setup;

public static class PermissionDependencySeeder
{
    public static async Task SeedAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger)
    {
        // todo: X service permission dependent Y operation permission
        // todo: Y operation permission dependent Z operation permission

        // update changes
        await db.SaveChangesAsync();
    }
}