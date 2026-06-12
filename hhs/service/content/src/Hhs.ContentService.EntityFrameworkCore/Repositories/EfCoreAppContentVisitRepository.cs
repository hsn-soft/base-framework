using System.Linq.Expressions;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Models;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreAppContentVisitRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<AppContentVisit, Guid>(provider, dbContext),
    IAppContentVisitRepository
{
    public async Task<AppContentVisit> CreateAsync(Guid customerId, ProductTypes productType, Guid appContentId, string visitResponse)
    {
        var newEntity = new AppContentVisit(
            id: Guid.CreateVersion7(),
            customerId: customerId,
            productType: productType,
            appContentId: appContentId,
            visitResponse: visitResponse
        );
        _ = await InsertAsync(newEntity);
        return newEntity;
    }

    public async Task<List<AppContentVisitCountModel>> GetContentIdsVisitCountsAsync(List<Guid> contentIds, bool? isMaxCountOrdered = null, long? contentOrderedLimit = null, long? contentVisitedCountLimit = null,
        CancellationToken cancellationToken = default)
    {
        if (contentIds is not { Count: > 0 }) return [];

        var query = from o in GetDbSet()
            where contentIds.Contains(o.AppContentId)
            group o by o.AppContentId
            into grouped
            select new AppContentVisitCountModel { AppContentId = grouped.Key, VisitCount = grouped.Count() };

        return await BaseGetContentIdsVisitCountsAsync(query, isMaxCountOrdered, contentOrderedLimit, contentVisitedCountLimit, cancellationToken);
    }

    public async Task BulkDeleteAsync(
        Expression<Func<AppContentVisit, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var delList = await GetDbSet().Where(predicate).AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
        if (delList is { Count: > 0 })
            await BulkDeleteAsync(delList, bulkConfig => { bulkConfig.UseTempDB = false; }, cancellationToken: cancellationToken);
    }

    private async Task<List<AppContentVisitCountModel>> BaseGetContentIdsVisitCountsAsync(IQueryable<AppContentVisitCountModel> query, bool? isMaxCountOrdered = null, long? contentOrderedLimit = null, long? contentVisitedCountLimit = null,
        CancellationToken cancellationToken = default)
    {
        if (contentVisitedCountLimit is > 0)
        {
            query = query.Where(x => x.VisitCount >= contentVisitedCountLimit.Value);
        }

        if (isMaxCountOrdered.HasValue && isMaxCountOrdered.Value)
        {
            query = query.OrderByDescending(x => x.VisitCount);
        }

        if (contentOrderedLimit is > 0)
        {
            query = query.Take((int)contentOrderedLimit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}