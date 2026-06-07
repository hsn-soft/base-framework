using System.Linq.Expressions;
using Hhs.ContentService.Domain.DashboardDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.DashboardDomain.Repositories;

public interface IResponseStatisticRepository : IReadOnlyGenericRepository<ResponseStatistic, Guid>
{
    Task<ResponseStatistic> CreateAsync(Guid id, Guid tenantId, Guid clientId, [NotNull] string responseStatus, ulong responseTime, ulong responseCount);
    Task<ResponseStatistic> CreateAsync(Guid tenantId, Guid clientId, [NotNull] string responseStatus, ulong responseTime, ulong responseCount);

    Task BulkDeleteAsync([NotNull] Expression<Func<ResponseStatistic, bool>> predicate, CancellationToken cancellationToken = default);
}