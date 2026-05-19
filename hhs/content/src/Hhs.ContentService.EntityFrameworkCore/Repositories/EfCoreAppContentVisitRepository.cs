using System.Globalization;
using System.Linq.Expressions;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Models;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreAppContentVisitRepository : EfCoreGenericRepository<AppContentVisit, Guid>, IAppContentVisitRepository
{
    public EfCoreAppContentVisitRepository(IServiceProvider provider, ContentServiceDbContext dbContext) : base(provider, dbContext)
    {
        // DefaultPropertySelector = null;
    }

    public async Task<AppContentVisit> CreateAsync(Guid clientId, Guid appContentId, string visitResponse)
    {
        var newEntity = new AppContentVisit(
            id: Guid.NewGuid(),
            clientId: clientId,
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

    private IQueryable<AppContentVisit> ApplyFilter(
        IQueryable<AppContentVisit> query,
        [CanBeNull] string searchText = null,
        Guid? clientId = null,
        uint? visitStartTime = null,
        uint? visitEndTime = null,
        [CanBeNull] string visitResponse = null
    )
    {
        searchText = searchText?.ToLower(new CultureInfo("en-US"));
        visitResponse = visitResponse?.ToLower(new CultureInfo("en-US"));

        // check start and end date value
        if (visitStartTime != null && visitEndTime != null && visitStartTime.Value > visitEndTime.Value)
        {
            uint tmp = visitStartTime.Value;
            visitStartTime = visitEndTime.Value;
            visitEndTime = tmp;
        }

        if (visitEndTime != null)
        {
            // limit end date
            query = query.Where(x => x.VisitTimeLine < visitEndTime.Value + 1);
        }

        if (visitStartTime != null)
        {
            // limit start date
            query = query.Where(x => x.VisitTimeLine >= visitStartTime.Value);
        }

        return query
            .WhereIf(!string.IsNullOrWhiteSpace(searchText), e => e.VisitResponse.Contains(searchText))
            .WhereIf(clientId.HasValue, e => e.ClientId == clientId.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(visitResponse), e => e.VisitResponse.Equals(visitResponse));
    }
}