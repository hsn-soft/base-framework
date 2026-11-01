using HsnSoft.Base.Domain.Sorting;
using Hsx.ContentService.Domain.CustomerContentDomain.Entities;

namespace Hsx.ContentService.Domain.CustomerContentDomain.Repositories;

/// <summary>The fields a list of customer contents may be sorted by and the sorting when it sends none.</summary>
public static class CustomerContentSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<CustomerContent> Fields = new SortFields<CustomerContent>()
        .Add("contentKey", entity => entity.NormalizedContentKey)
        .Add("slugKey", entity => entity.SlugKey)
        .Add("createdAt", entity => entity.CreatedAt);
}
