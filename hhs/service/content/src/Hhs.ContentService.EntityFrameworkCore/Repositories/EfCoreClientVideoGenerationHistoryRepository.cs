using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreCustomerVideoGenerationHistoryRepository : EfCoreGenericRepository<CustomerVideoGenerationHistory, Guid>, ICustomerVideoGenerationHistory
{
    public EfCoreCustomerVideoGenerationHistoryRepository(IServiceProvider provider, ContentServiceDbContext dbContext) : base(provider, dbContext)
    {
        // DefaultPropertySelector = null;
    }

    public async Task<long> GetCustomerVideoHistoryCountAsync(Guid customerId, DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null)
        => videoGenerationType == null
            ? await GetCountAsync(x => x.CustomerId == customerId && x.VideoGenerationDate == videoGenerationDate)
            : await GetCountAsync(x => x.CustomerId == customerId && x.VideoGenerationDate == videoGenerationDate && x.VideoGenerationType == videoGenerationType);

    public async Task<CustomerVideoGenerationHistory> CreateAsync(Guid tenantId, Guid customerId, DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, string contentReferenceIds)
    {
        var newEntity = new CustomerVideoGenerationHistory(
            id: Guid.CreateVersion7(),
            tenantId: tenantId,
            customerId: customerId,
            videoGenerationDate: videoGenerationDate,
            videoGenerationType: videoGenerationType,
            contentReferenceIds: contentReferenceIds
        );
        _ = await InsertAsync(newEntity);
        return newEntity;
    }
}