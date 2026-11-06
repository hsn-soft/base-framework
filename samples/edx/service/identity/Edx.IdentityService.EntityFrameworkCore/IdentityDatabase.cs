using Edx.IdentityService.Domain.IdentityDomain.Consts;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Edx.Shared.Helper;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore;

public static class IdentityDatabase
{
    // the tenants of the sample and the tree they make: the system tenant and Techsummus are at the root, Veriban and Bilgera are the customers of Techsummus (a parent is listed before its children)
    internal static readonly (Guid Id, string Code, string Name, Guid? ParentId)[] s_tenants =
    [
        (EdxTenants.System, EdxTenants.SystemCode, "System", null),
        (EdxTenants.Techsummus, EdxTenants.TechsummusCode, "Techsummus", null),
        (EdxTenants.Veriban, EdxTenants.VeribanCode, "Veriban", EdxTenants.Techsummus),
        (EdxTenants.Bilgera, EdxTenants.BilgeraCode, "Bilgera", EdxTenants.Techsummus),
    ];

    /// <summary>
    /// Makes the tenants that are missing (<c>system-tenant</c>, <c>techsummus</c>, <c>veriban</c>, <c>bilgera</c>, with the ids of <c>EdxTenants</c>) and puts them in the tree: Veriban and Bilgera are under Techsummus. A tenant that was made before the
    /// tree (its path is empty) is placed; one that has a place is left where it is, so what an administrator moved is not moved back. The roles and the users of the sample are made by <see cref="SampleIdentity.EnsureSampleIdentityAsync"/>, after the
    /// permissions (a role is given its starting grants when it is made). Only the identity service does this: the database is the identity service's, the auth server only reads it (so it needs the identity service to have started once).
    /// </summary>
    internal static async Task EnsureTenantsAsync(IdentityDbContext db, CancellationToken cancellationToken)
    {
        Dictionary<Guid, Tenant> known = await db.Tenants.ToDictionaryAsync(tenant => tenant.Id, cancellationToken).ConfigureAwait(false);
        foreach ((Guid id, string code, string name, Guid? parentId) in s_tenants)
        {
            Tenant? parent = parentId is { } parentKey ? known[parentKey] : null;
            if (!known.TryGetValue(id, out Tenant? tenant))
            {
                tenant = Tenant.Create(id, code, name, parent);
                db.Tenants.Add(tenant);
                known[id] = tenant;
            }
            else if (!TenantAccessPath.IsValid(tenant.AccessPath))
            {
                tenant.PlaceUnder(parent);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds the two columns of the tree (<c>ParentId</c>, <c>AccessPath</c>) to a <c>Tenant</c> table that was made before them: <c>EnsureCreated</c> makes the tables of a database that is not there and does nothing to one that is, so a database of an
    /// earlier version would not have them. It changes nothing in a database that has them (a new one has: the model makes them), and runs before the seeders, so the tenants are placed in the tree after. SQL Server, the provider of this database.
    /// </summary>
    internal static async Task EnsureTreeColumnsAsync(IdentityDbContext db, CancellationToken cancellationToken)
    {
        string table = $"[{EfCoreIdentityDbProperties.DbSchema ?? "dbo"}].[{EfCoreIdentityDbProperties.DbTablePrefix}{IdentityConsts.TenantTable}]";
#pragma warning disable EF1002, EF1003 // the names are constants of this project, not input
        await db.Database.ExecuteSqlRawAsync(
            $"IF COL_LENGTH('{table}', 'ParentId') IS NULL ALTER TABLE {table} ADD [ParentId] uniqueidentifier NULL; "
            + $"IF COL_LENGTH('{table}', 'AccessPath') IS NULL ALTER TABLE {table} ADD [AccessPath] nvarchar({IdentityConsts.AccessPathMaxLength}) NOT NULL CONSTRAINT [DF_Tenant_AccessPath] DEFAULT '';",
            cancellationToken).ConfigureAwait(false);
        await db.Database.ExecuteSqlRawAsync(
            $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tenant_AccessPath' AND object_id = OBJECT_ID('{table}')) CREATE INDEX [IX_Tenant_AccessPath] ON {table} ([AccessPath]); "
            + $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tenant_ParentId' AND object_id = OBJECT_ID('{table}')) CREATE INDEX [IX_Tenant_ParentId] ON {table} ([ParentId]);",
            cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1002, EF1003
    }
}
