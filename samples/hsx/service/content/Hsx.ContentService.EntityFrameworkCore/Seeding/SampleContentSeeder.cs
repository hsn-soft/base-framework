using HsnSoft.Base.Data;
using Hsx.ContentService.Domain.CustomerContentDomain.Entities;
using Hsx.ContentService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Seeding;

/// <summary>
/// Demo contents of the one demo customer, under the scope keys of its two subscriptions (the video platform Techsummus sold and the vertical video Pixel sold; the identity service puts the same keys into the tokens). Run again, it adds only
/// what is missing.
/// </summary>
internal sealed class SampleContentSeeder(ContentDbContext db) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach ((string scope, string prefix) in new[] { (HsxSeed.VideoPlatformScope, "vp"), (HsxSeed.VerticalVideoScope, "cc") })
        {
            for (int i = 1; i <= 3; i++)
            {
                string key = $"{prefix}-page-{i}";
                string normalized = HsnSoft.Base.Text.StringHelper.Normalize(key);
                if (!await db.CustomerContents.IgnoreQueryFilters().AnyAsync(content => content.ScopeKey == scope && content.NormalizedContentKey == normalized, cancellationToken).ConfigureAwait(false))
                {
                    db.CustomerContents.Add(CustomerContent.Create(scope, key, key, "Completed", i == 1 ? "Ready" : "Pending", null, null, null));
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
