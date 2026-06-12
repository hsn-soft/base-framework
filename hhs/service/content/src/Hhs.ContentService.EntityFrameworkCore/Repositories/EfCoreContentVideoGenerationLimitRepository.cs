using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreContentVideoGenerationLimitRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<ContentVideoGenerationLimit, Guid>(provider, dbContext), IContentVideoGenerationLimitRepository
{
    public async Task<long> GetCustomerVideoHistoryCountAsync(string scopeKey,
        DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null) =>
        videoGenerationType == null
            ? await GetCountAsync(x => x.ScopeKey == scopeKey && x.VideoGenerationDate == videoGenerationDate)
            : await GetCountAsync(x => x.ScopeKey == scopeKey && x.VideoGenerationDate == videoGenerationDate && x.VideoGenerationType == videoGenerationType);

    public async Task<ContentVideoGenerationLimit> CreateAsync(string scopeKey,
        DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, string contentReferenceIds)
    {
        var newEntity = new ContentVideoGenerationLimit(
            id: Guid.CreateVersion7(),
            scopeKey: scopeKey,
            videoGenerationDate: videoGenerationDate,
            videoGenerationType: videoGenerationType,
            contentReferenceIds: contentReferenceIds
        );
        _ = await InsertAsync(newEntity);
        return newEntity;
    }
}