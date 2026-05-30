using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoNormalizedRequestRepository : MongoGenericRepository<NormalizedRequest, Guid>, INormalizedRequestRepository
{
    public MongoNormalizedRequestRepository(IServiceProvider provider, TextNormalizerServiceDbContext dbContext) : base(provider, dbContext)
    {
    }

    public async Task<NormalizedRequest> CreateAsync(
        Guid tenantId,
        Guid clientId,
        string domainName,
        Guid appContentId,
        string domainPath,
        NormalizedRequestStates operationStatus,
        string operationStatusDescription = null,
        ScrapingContentDataModel scrapingContentData = null,
        string outlineContentData = null,
        string correlationId = null)
        => await CreateAsync(id: Guid.CreateVersion7(),
            tenantId: tenantId,
            clientId: clientId,
            domainName: domainName,
            appContentId: appContentId,
            domainPath: domainPath,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            scrapingContentData: scrapingContentData,
            outlineContentData: outlineContentData,
            correlationId: correlationId
        );

    public async Task<NormalizedRequest> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        string domainName,
        Guid appContentId,
        string domainPath,
        NormalizedRequestStates operationStatus,
        string operationStatusDescription = null,
        ScrapingContentDataModel scrapingContentData = null,
        string outlineContentData = null,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        var draft = new NormalizedRequest(
            id: id,
            tenantId: tenantId,
            clientId: clientId,
            domainName: domainName,
            appContentId: appContentId,
            domainPath: domainPath,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            scrapingContentData: scrapingContentData,
            outlineContentData: outlineContentData,
            correlationId: correlationId
        );

        //Domain Rules
        // Rule01
        // Rule02
        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task<NormalizedRequest> SetScrapingResultAsync(Guid id, bool isScrapingSuccess, string errorMessage, ScrapingContentDataModel scrapingContentData)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new NormalizedRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != NormalizedRequestStates.CreatedWaitForScraping)
        {
            throw new NormalizedRequestStateException(id.ToString());
        }

        if (isScrapingSuccess)
        {
            oldEntity.OperationStatus = NormalizedRequestStates.ContentScrappedWaitForOutline;
            oldEntity.OperationStatusDescription = NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_SCRAPING_SUCCESS;
            oldEntity.SetScrapingContentData(scrapingContentData);
        }
        else
        {
            oldEntity.OperationStatus = NormalizedRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_SCRAPING_FAIL + " " + errorMessage;
            oldEntity.SetScrapingContentData(null);
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<NormalizedRequest> SetOutlineResultAsync(Guid id, bool isNormalizedSuccess, string errorMessage, string outlineResult)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new NormalizedRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != NormalizedRequestStates.ContentScrappedWaitForOutline)
        {
            throw new NormalizedRequestStateException(id.ToString());
        }

        if (isNormalizedSuccess)
        {
            oldEntity.OperationStatus = NormalizedRequestStates.OperationSuccess;
            oldEntity.OperationStatusDescription = NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_OUTLINE_SUCCESS;
            oldEntity.OutlineContentData = outlineResult;
        }
        else
        {
            oldEntity.OperationStatus = NormalizedRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_OUTLINE_FAIL + " " + errorMessage;
            oldEntity.OutlineContentData = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<NormalizedRequest> SetStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new NormalizedRequestNotFoundException(id.ToString());
        }

        oldEntity.OperationStatus = NormalizedRequestStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<NormalizedRequest> FindByUniqueKeysAsync(Guid clientId, Guid appContentId, CancellationToken cancellationToken = default)
    {
        return await GetSingleOrDefaultAsync(x => x.ClientId == clientId && x.AppContentId == appContentId, cancellationToken: cancellationToken);
    }
}