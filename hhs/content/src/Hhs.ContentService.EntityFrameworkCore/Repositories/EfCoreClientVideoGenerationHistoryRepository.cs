using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ClientDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreClientVideoGenerationHistoryRepository : EfCoreGenericRepository<ClientVideoGenerationHistory, Guid>, IClientVideoGenerationHistoryRepository
{
    public EfCoreClientVideoGenerationHistoryRepository(IServiceProvider provider, ContentServiceDbContext dbContext) : base(provider, dbContext)
    {
        // DefaultPropertySelector = null;
    }

    public async Task<long> GetClientVideoHistoryCountAsync(Guid clientId, DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null)
        => videoGenerationType == null
            ? await GetCountAsync(x => x.ClientId == clientId && x.VideoGenerationDate == videoGenerationDate)
            : await GetCountAsync(x => x.ClientId == clientId && x.VideoGenerationDate == videoGenerationDate && x.VideoGenerationType == videoGenerationType);

    public async Task<ClientVideoGenerationHistory> CreateAsync(Guid clientId, DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, string contentReferenceIds)
    {
        var newEntity = new ClientVideoGenerationHistory(
            id: Guid.CreateVersion7(),
            clientId: clientId,
            videoGenerationDate: videoGenerationDate,
            videoGenerationType: videoGenerationType,
            contentReferenceIds: contentReferenceIds
        );
        _ = await InsertAsync(newEntity);
        return newEntity;
    }
}