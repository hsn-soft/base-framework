using System.Linq.Expressions;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Models;
using Hhs.ContentService.Domain.SettingDomain.Models;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface ICustomerContentVisitRepository : IGenericRepository<CustomerContentVisit, Guid>
{
    Task<CustomerContentVisit> CreateAsync(
        [NotNull] string scopeKey,
        Guid customerContentId,
        [NotNull] string visitResponse
    );

    Task<List<CustomerContentVisitCountModel>> GetContentIdsVisitCountsAsync(
        List<Guid> customerContentIds,
        bool? isMaxCountOrdered = null,
        long? customerContentOrderedLimit = null,
        long? customerContentVisitedCountLimit = null,
        CancellationToken cancellationToken = default
    );

    Task BulkDeleteAsync([NotNull] Expression<Func<CustomerContentVisit, bool>> predicate, CancellationToken cancellationToken = default);
}