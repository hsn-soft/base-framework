using System.Linq.Expressions;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Models;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IAppContentVisitRepository : IReadOnlyGenericRepository<AppContentVisit, Guid>
{
    Task<AppContentVisit> CreateAsync(Guid clientId, Guid appContentId, [NotNull] string visitResponse);

    Task<List<AppContentVisitCountModel>> GetContentIdsVisitCountsAsync(List<Guid> contentIds, bool? isMaxCountOrdered = null, long? contentOrderedLimit = null, long? contentVisitedCountLimit = null,
        CancellationToken cancellationToken = default);

    Task BulkDeleteAsync([NotNull] Expression<Func<AppContentVisit, bool>> predicate, CancellationToken cancellationToken = default);
}