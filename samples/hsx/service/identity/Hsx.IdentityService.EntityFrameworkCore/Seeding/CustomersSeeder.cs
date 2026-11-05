using HsnSoft.Base.Data;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>
/// The customers (a domain of a company) every environment starts with, under fixed ids. After the companies, which they belong to. A customer that is there (even one that was deleted, which the filter hides) is left as it is. The normalized
/// domain is made by the entity, not given.
/// </summary>
internal sealed class CustomersSeeder(IdentityServiceDbContext db) : IDataSeeder
{
    private static readonly (string Id, string CompanyId, string Domain)[] s_defaults =
    [
        ("00000000-0000-7000-8000-00000000b002", "00000000-0000-7000-8000-00000000b001", "cnbce.com"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        HashSet<Guid> known = [.. await db.Set<Customer>().IgnoreQueryFilters().Select(customer => customer.Id).ToListAsync(cancellationToken).ConfigureAwait(false)];
        db.Set<Customer>().AddRange(s_defaults.Select(customer => (Id: Guid.Parse(customer.Id), CompanyId: Guid.Parse(customer.CompanyId), customer.Domain)).Where(customer => !known.Contains(customer.Id)).Select(customer => Customer.Create(customer.CompanyId, customer.Domain, customer.Id)));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
