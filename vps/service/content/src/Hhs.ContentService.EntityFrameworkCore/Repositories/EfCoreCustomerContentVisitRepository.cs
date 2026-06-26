using System.Linq.Expressions;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Models;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.SettingDomain.Models;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreCustomerContentVisitRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<CustomerContentVisit, Guid>(provider, dbContext),
    ICustomerContentVisitRepository
{
    public async Task<CustomerContentVisit> CreateAsync(
        string scopeKey,
        Guid customerContentId,
        string visitResponse
    )
    {
        var newEntity = new CustomerContentVisit(
            id: Guid.CreateVersion7(),
            scopeKey: scopeKey,
            customerContentId: customerContentId,
            visitResponse: visitResponse
        );
        _ = await InsertAsync(newEntity);
        return newEntity;
    }

    public async Task<List<CustomerContentVisitCountModel>> GetContentIdsVisitCountsAsync(
        List<Guid> customerContentIds,
        bool? isMaxCountOrdered = null,
        long? customerContentOrderedLimit = null,
        long? customerContentVisitedCountLimit = null,
        CancellationToken cancellationToken = default
    )
    {
        if (customerContentIds is not { Count: > 0 }) return [];

        var query = from o in GetDbSet()
            where customerContentIds.Contains(o.CustomerContentId)
            group o by o.CustomerContentId
            into grouped
            select new CustomerContentVisitCountModel { CustomerContentId = grouped.Key, VisitCount = grouped.Count() };

        return await BaseGetContentIdsVisitCountsAsync(query,
            isMaxCountOrdered,
            customerContentOrderedLimit,
            customerContentVisitedCountLimit,
            cancellationToken
        );
    }

    public async Task BulkDeleteAsync(Expression<Func<CustomerContentVisit, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var delList = await GetDbSet().Where(predicate).AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
        if (delList is { Count: > 0 })
            await BulkDeleteAsync(delList, bulkConfig => { bulkConfig.UseTempDB = false; }, cancellationToken: cancellationToken);
    }

    private async Task<List<CustomerContentVisitCountModel>> BaseGetContentIdsVisitCountsAsync(
        IQueryable<CustomerContentVisitCountModel> query,
        bool? isMaxCountOrdered = null,
        long? customerContentOrderedLimit = null,
        long? customerContentVisitedCountLimit = null,
        CancellationToken cancellationToken = default
    )
    {
        if (customerContentVisitedCountLimit is > 0)
        {
            query = query.Where(x => x.VisitCount >= customerContentVisitedCountLimit.Value);
        }

        if (isMaxCountOrdered.HasValue && isMaxCountOrdered.Value)
        {
            query = query.OrderByDescending(x => x.VisitCount);
        }

        if (customerContentOrderedLimit is > 0)
        {
            query = query.Take((int)customerContentOrderedLimit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}