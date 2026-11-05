using HsnSoft.Base.Data;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>
/// The companies every environment starts with, under fixed ids (the customers name them). A company that is there (even one that was deleted, which the filter hides) is left as it is, so what the system tenant changed is not undone at the next
/// start. The normalized name is made by the entity, not given.
/// </summary>
internal sealed class CompaniesSeeder(IdentityServiceDbContext db) : IDataSeeder
{
    private static readonly (string Id, string Title, string Name)[] s_defaults =
    [
        ("00000000-0000-7000-8000-00000000b001", "Ilbak", "ilbak"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        HashSet<Guid> known = [.. await db.Set<Company>().IgnoreQueryFilters().Select(company => company.Id).ToListAsync(cancellationToken).ConfigureAwait(false)];
        db.Set<Company>().AddRange(s_defaults.Select(company => (Id: Guid.Parse(company.Id), company.Title, company.Name)).Where(company => !known.Contains(company.Id)).Select(company => Company.Create(company.Title, company.Name, company.Id)));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
