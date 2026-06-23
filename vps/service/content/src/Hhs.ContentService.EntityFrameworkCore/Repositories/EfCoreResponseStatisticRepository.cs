namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

// public sealed class EfCoreResponseStatisticRepository : EfCoreGenericRepository<ResponseStatistic, Guid>, IResponseStatisticRepository
// {
//     [NotNull] protected IStringLocalizer L { get; }
//
//     public EfCoreResponseStatisticRepository(IServiceProvider provider, IStringLocalizerFactory stringLocalizerFactory, ContentServiceDbContext dbContext) : base(provider, dbContext)
//     {
//         // DefaultPropertySelector = null;
//
//         L = stringLocalizerFactory.CreateMultiple([typeof(ContentServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
//     }
//
//     public async Task<ResponseStatistic> CreateAsync(Guid tenantId, Guid clientId, string responseStatus, ulong responseTime, ulong responseCount)
//         => await CreateAsync(id: Guid.CreateVersion7(),
//             tenantId: tenantId,
//             clientId: clientId,
//             responseStatus: responseStatus,
//             responseTime: responseTime,
//             responseCount: responseCount
//         );
//
//     public async Task<ResponseStatistic> CreateAsync(Guid id, Guid tenantId, Guid clientId, string responseStatus, ulong responseTime, ulong responseCount)
//     {
//         if (id == Guid.Empty) id = Guid.CreateVersion7();
//
//         // Create draft
//         var draft = new ResponseStatistic(
//             id: id,
//             tenantId: tenantId,
//             clientId: clientId,
//             responseStatus: responseStatus,
//             responseTime: responseTime,
//             responseCount: responseCount
//         );
//
//         //Domain Rules
//         _ = await InsertAsync(draft);
//         return draft;
//     }
//
//     public async Task BulkDeleteAsync(Expression<Func<ResponseStatistic, bool>> predicate, CancellationToken cancellationToken = default)
//     {
//         var delList = await GetDbSet().Where(predicate).AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
//         if (delList is { Count: > 0 })
//             await BulkDeleteAsync(delList, bulkConfig => { bulkConfig.UseTempDB = false; }, cancellationToken: cancellationToken);
//     }
//
//     private IQueryable<ResponseStatistic> ApplyFilter(
//         IQueryable<ResponseStatistic> query,
//         [CanBeNull] string searchText = null,
//         Guid? clientId = null,
//         ulong? responseStartTime = null,
//         ulong? responseEndTime = null,
//         [CanBeNull] string responseStatus = null
//     )
//     {
//         searchText = searchText?.ToLower(new CultureInfo("en-US"));
//         responseStatus = responseStatus?.ToLower(new CultureInfo("en-US"));
//
//         // check start and end date value
//         if (responseStartTime != null && responseEndTime != null && responseStartTime.Value > responseEndTime.Value)
//         {
//             ulong tmp = responseStartTime.Value;
//             responseStartTime = responseEndTime.Value;
//             responseEndTime = tmp;
//         }
//
//         if (responseEndTime != null)
//         {
//             // limit end date
//             query = query.Where(x => x.ResponseTime < responseEndTime.Value + 1);
//         }
//
//         if (responseStartTime != null)
//         {
//             // limit start date
//             query = query.Where(x => x.ResponseTime >= responseStartTime.Value);
//         }
//
//         return query
//             .WhereIf(!string.IsNullOrWhiteSpace(searchText), e => e.ResponseStatus.Contains(searchText))
//             .WhereIf(clientId.HasValue, e => e.ClientId == clientId.Value)
//             .WhereIf(!string.IsNullOrWhiteSpace(responseStatus), e => e.ResponseStatus.Equals(responseStatus));
//     }
// }