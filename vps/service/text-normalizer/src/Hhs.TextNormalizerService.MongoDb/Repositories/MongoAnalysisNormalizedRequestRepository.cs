using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts.Facilities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoAnalysisNormalizedRequestRepository(
    IServiceProvider provider,
    TextNormalizerServiceDbContext dbContext
) : MongoGenericRepository<AnalysisNormalizedRequest, Guid>(provider, dbContext), IAnalysisNormalizedRequestRepository
{
    public async Task<AnalysisNormalizedRequest> CreateAsync(
        string scopeKey,
        string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        List<AnalysisReferenceModel> analysisReferenceList,
        AnalysisNormalizedRequestStates operationStatus,
        string operationStatusDescription = null,
        string correlationId = null)
        => await CreateAsync(id: Guid.CreateVersion7(),
            scopeKey: scopeKey,
            domainName: domainName,
            analysisContentId: analysisContentId,
            analysisDate: analysisDate,
            analysisReferenceList: analysisReferenceList,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            correlationId: correlationId
        );

    public async Task<AnalysisNormalizedRequest> CreateAsync(
        Guid id,
        string scopeKey,
        string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        List<AnalysisReferenceModel> analysisReferenceList,
        AnalysisNormalizedRequestStates operationStatus,
        string operationStatusDescription = null,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        var draft = new AnalysisNormalizedRequest(
            id: id,
            scopeKey: scopeKey,
            domainName: domainName,
            analysisContentId: analysisContentId,
            analysisDate: analysisDate,
            analysisReferenceList: analysisReferenceList,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            correlationId: correlationId
        );

        //Domain Rules
        // Rule01
        // Rule02

        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task<AnalysisNormalizedRequest> SetOutlineResultAsync(Guid id, bool isOutlineSuccess, string errorMessage, List<KeyValuePair<Guid, string>> outlineResultList)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new AnalysisNormalizedRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != AnalysisNormalizedRequestStates.CreatedWaitForOutline)
        {
            throw new AnalysisNormalizedRequestStateException(id.ToString());
        }

        if (isOutlineSuccess)
        {
            oldEntity.OperationStatus = AnalysisNormalizedRequestStates.OperationSuccess;
            oldEntity.OperationStatusDescription = AnalysisNormalizedRequestOperationFacilities.ANALYSIS_NORMALIZED_REQUEST_OUTLINE_SUCCESS;

            if (oldEntity.AnalysisReferenceList is { Count: > 0 } && outlineResultList?.Count == oldEntity.AnalysisReferenceList.Count)
            {
                foreach (var analysisReference in oldEntity.AnalysisReferenceList)
                {
                    analysisReference.OutlineContentData = outlineResultList.Find(x => x.Key == analysisReference.CustomerContentId).Value;
                }
            }
            else
            {
                oldEntity.OperationStatus = AnalysisNormalizedRequestStates.OperationFail;
                oldEntity.OperationStatusDescription =
                    AnalysisNormalizedRequestOperationFacilities.ANALYSIS_NORMALIZED_REQUEST_OUTLINE_FAIL
                    + "UNEXPECTED_SPLITTED_CONTENT_COUNT: EXPECTED(" + oldEntity.AnalysisReferenceList.Count
                    + "), ACTUAL(" + outlineResultList?.Count + ")";
            }
        }
        else
        {
            oldEntity.OperationStatus = AnalysisNormalizedRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = AnalysisNormalizedRequestOperationFacilities.ANALYSIS_NORMALIZED_REQUEST_OUTLINE_FAIL + " " + errorMessage;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<AnalysisNormalizedRequest> SetStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new AnalysisNormalizedRequestNotFoundException(id.ToString());
        }

        oldEntity.OperationStatus = AnalysisNormalizedRequestStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }
}