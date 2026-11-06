using HsnSoft.Base.Domain.Sorting;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Entities;

namespace Hsx.ContentService.Domain.CustomerContentVisitDomain.Repositories;

/// <summary>The fields a list of customer content visits may be sorted by and the sorting when it sends none.</summary>
public static class CustomerContentVisitSorting
{
    public const string Default = "visitTime desc";

    public static readonly SortFields<CustomerContentVisit> Fields = new SortFields<CustomerContentVisit>()
        .Add("visitTime", entity => entity.VisitTime);
}
