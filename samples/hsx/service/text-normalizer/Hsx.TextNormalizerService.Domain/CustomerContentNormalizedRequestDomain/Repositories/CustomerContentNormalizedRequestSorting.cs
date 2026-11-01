using HsnSoft.Base.Domain.Sorting;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Entities;

namespace Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Repositories;

/// <summary>The fields a list of customer content normalized requests may be sorted by and the sorting when it sends none.</summary>
public static class CustomerContentNormalizedRequestSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<CustomerContentNormalizedRequest> Fields = new SortFields<CustomerContentNormalizedRequest>()
        .Add("domainName", entity => entity.DomainName)
        .Add("contentKey", entity => entity.NormalizedContentKey)
        .Add("createdAt", entity => entity.CreatedAt);
}
