using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts.Facilities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoContentNormalizedRequestRepository(
    IServiceProvider provider,
    TextNormalizerServiceDbContext dbContext
) : MongoGenericRepository<ContentNormalizedRequest, Guid>(provider, dbContext), IContentNormalizedRequestRepository
{
    public async Task<ContentNormalizedRequest> CreateAsync(
        string scopeKey,
        string domainName,
        Guid customerContentId,
        string domainPath,
        ContentNormalizedRequestStates operationStatus,
        string operationStatusDescription = null,
        ScrapingContentDataModel scrapingContentData = null,
        string outlineContentData = null,
        string correlationId = null)
        => await CreateAsync(id: Guid.CreateVersion7(),
            scopeKey: scopeKey,
            domainName: domainName,
            customerContentId: customerContentId,
            domainPath: domainPath,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            scrapingContentData: scrapingContentData,
            outlineContentData: outlineContentData,
            correlationId: correlationId
        );

    public async Task<ContentNormalizedRequest> CreateAsync(
        Guid id,
        string scopeKey,
        string domainName,
        Guid customerContentId,
        string domainPath,
        ContentNormalizedRequestStates operationStatus,
        string operationStatusDescription = null,
        ScrapingContentDataModel scrapingContentData = null,
        string outlineContentData = null,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        var draft = new ContentNormalizedRequest(
            id: id,
            scopeKey: scopeKey,
            domainName: domainName,
            customerContentId: customerContentId,
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

    public async Task<ContentNormalizedRequest> SetScrapingResultAsync(Guid id, bool isScrapingSuccess, string errorMessage, ScrapingContentDataModel scrapingContentData)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new ContentNormalizedRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != ContentNormalizedRequestStates.CreatedWaitForScraping)
        {
            throw new ContentNormalizedRequestStateException(id.ToString());
        }

        if (isScrapingSuccess)
        {
            oldEntity.OperationStatus = ContentNormalizedRequestStates.ContentScrappedWaitForOutline;
            oldEntity.OperationStatusDescription = ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_SCRAPING_SUCCESS;
            oldEntity.SetScrapingContentData(scrapingContentData);
        }
        else
        {
            oldEntity.OperationStatus = ContentNormalizedRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_SCRAPING_FAIL + " " + errorMessage;
            oldEntity.SetScrapingContentData(null);
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<ContentNormalizedRequest> SetOutlineResultAsync(Guid id, bool isNormalizedSuccess, string errorMessage, string outlineResult)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new ContentNormalizedRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != ContentNormalizedRequestStates.ContentScrappedWaitForOutline)
        {
            throw new ContentNormalizedRequestStateException(id.ToString());
        }

        if (isNormalizedSuccess)
        {
            oldEntity.OperationStatus = ContentNormalizedRequestStates.OperationSuccess;
            oldEntity.OperationStatusDescription = ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_OUTLINE_SUCCESS;
            oldEntity.OutlineContentData = outlineResult;
        }
        else
        {
            oldEntity.OperationStatus = ContentNormalizedRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_OUTLINE_FAIL + " " + errorMessage;
            oldEntity.OutlineContentData = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<ContentNormalizedRequest> SetStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new ContentNormalizedRequestNotFoundException(id.ToString());
        }

        oldEntity.OperationStatus = ContentNormalizedRequestStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }
}