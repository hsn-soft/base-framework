using HsnSoft.Base.Data;
using Hsx.IdentityService.Domain.PermissionDomain.Catalog;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.IdentityService.EntityFrameworkCore.Permissions;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>The permissions of the catalogue and the menu (<see cref="PermissionDatabase.EnsurePermissionsAsync"/>): the code says what they are, and the table is brought up to date at every start. Before the roles, which are granted their starting grants when they are made.</summary>
internal sealed class PermissionsSeeder(IdentityServiceDbContext db, IPermissionCatalog catalog) : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken) => PermissionDatabase.EnsurePermissionsAsync(db, catalog, TimeProvider.System, cancellationToken);
}
