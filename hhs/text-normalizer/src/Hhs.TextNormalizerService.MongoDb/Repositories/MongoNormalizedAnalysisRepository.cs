using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoNormalizedAnalysisRepository : MongoGenericRepository<NormalizedAnalysis, Guid>, INormalizedAnalysisRepository
{
    public MongoNormalizedAnalysisRepository(IServiceProvider provider, TextNormalizerServiceDbContext dbContext) : base(provider, dbContext)
    {
    }

    public async Task<NormalizedAnalysis> CreateAsync(
        Guid tenantId,
        Guid clientId,
        string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        List<AnalysisReferenceModel> analysisReferenceList,
        NormalizedAnalysisStates operationStatus,
        string operationStatusDescription = null,
        string correlationId = null)
        => await CreateAsync(id: Guid.CreateVersion7(),
            tenantId: tenantId,
            clientId: clientId,
            domainName: domainName,
            analysisContentId: analysisContentId,
            analysisDate: analysisDate,
            analysisReferenceList: analysisReferenceList,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            correlationId: correlationId
        );

    public async Task<NormalizedAnalysis> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        string domainName,
        Guid analysisContentId,
        DateTime analysisDate,
        List<AnalysisReferenceModel> analysisReferenceList,
        NormalizedAnalysisStates operationStatus,
        string operationStatusDescription = null,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        var draft = new NormalizedAnalysis(
            id: id,
            tenantId: tenantId,
            clientId: clientId,
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

    public async Task<NormalizedAnalysis> SetOutlineResultAsync(Guid id, bool isOutlineSuccess, string errorMessage, List<KeyValuePair<Guid, string>> outlineResultList)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new NormalizedAnalysisNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != NormalizedAnalysisStates.CreatedWaitForOutline)
        {
            throw new NormalizedAnalysisStateException(id.ToString());
        }

        if (isOutlineSuccess)
        {
            oldEntity.OperationStatus = NormalizedAnalysisStates.OperationSuccess;
            oldEntity.OperationStatusDescription = NormalizedAnalysisOperationFacilities.NORMALIZED_ANALYSIS_OUTLINE_SUCCESS;

            if (oldEntity.AnalysisReferenceList is { Count: > 0 } && outlineResultList?.Count == oldEntity.AnalysisReferenceList.Count)
            {
                foreach (var analysisReference in oldEntity.AnalysisReferenceList)
                {
                    analysisReference.OutlineContentData = outlineResultList.Find(x => x.Key == analysisReference.AppContentId).Value;
                }
            }
            else
            {
                oldEntity.OperationStatus = NormalizedAnalysisStates.OperationFail;
                oldEntity.OperationStatusDescription =
                    NormalizedAnalysisOperationFacilities.NORMALIZED_ANALYSIS_OUTLINE_FAIL
                    + "UNEXPECTED_SPLITTED_CONTENT_COUNT: EXPECTED(" + oldEntity.AnalysisReferenceList.Count
                    + "), ACTUAL(" + outlineResultList?.Count + ")";
            }
        }
        else
        {
            oldEntity.OperationStatus = NormalizedAnalysisStates.OperationFail;
            oldEntity.OperationStatusDescription = NormalizedAnalysisOperationFacilities.NORMALIZED_ANALYSIS_OUTLINE_FAIL + " " + errorMessage;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<NormalizedAnalysis> SetStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new NormalizedAnalysisNotFoundException(id.ToString());
        }

        oldEntity.OperationStatus = NormalizedAnalysisStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<NormalizedAnalysis> FindByUniqueKeysAsync(Guid clientId, Guid analysisContentId, CancellationToken cancellationToken = default)
    {
        return await GetSingleOrDefaultAsync(x => x.ClientId == clientId && x.AnalysisContentId == analysisContentId, cancellationToken: cancellationToken);
    }
}