using Edx.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Options;

namespace Edx.IdentityService.EntityFrameworkCore;

// The data the identity database starts with, in the order they are added (see AddIdentityPersistence). Each one only adds what is missing, so every pod of the owner and every start may run it; one that must not run in an
// environment (the people of the sample, in production) is listed in Seeding:Disabled by the name of its class.

/// <summary>The two columns of the tree of tenants, for a database that was made before them (<see cref="IdentityDatabase.EnsureTreeColumnsAsync"/>); first, so the tenants can be placed in the tree.</summary>
internal sealed class TenantTreeColumnsSeeder(IdentityDbContext db) : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken) => IdentityDatabase.EnsureTreeColumnsAsync(db, cancellationToken);
}

/// <summary>The tenants of the system and the tree they make (<see cref="IdentityDatabase.EnsureTenantsAsync"/>).</summary>
internal sealed class TenantsSeeder(IdentityDbContext db) : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken) => IdentityDatabase.EnsureTenantsAsync(db, cancellationToken);
}

/// <summary>The permissions of the catalogue and the menu (<see cref="PermissionDatabase.EnsurePermissionsAsync"/>); before the roles, which are granted them when they are made.</summary>
internal sealed class PermissionsSeeder(IdentityDbContext db, TimeProvider time) : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken) => PermissionDatabase.EnsurePermissionsAsync(db, time, cancellationToken);
}

/// <summary>The roles of every tenant and the people of the sample (<see cref="SampleIdentity.EnsureSampleIdentityAsync"/>); the passwords are the <c>Seed:Users</c> section's.</summary>
internal sealed class SampleIdentitySeeder(IdentityDbContext db, ICurrentTenant currentTenant, IOptions<IdentitySeedOptions> seed) : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken) => SampleIdentity.EnsureSampleIdentityAsync(db, currentTenant, seed.Value.Users, cancellationToken);
}
