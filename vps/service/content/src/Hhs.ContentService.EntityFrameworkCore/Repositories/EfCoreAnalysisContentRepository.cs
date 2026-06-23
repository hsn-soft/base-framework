using Hhs.ContentService.Domain.ContentDomain.Consts.Facilities;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Exceptions;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.Localization;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreAnalysisContentRepository(
    IServiceProvider provider,
    IStringLocalizerFactory stringLocalizerFactory,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<AnalysisContent, Guid>(provider, dbContext), IAnalysisContentRepository
{
    [NotNull]
    protected IStringLocalizer L { get; } = stringLocalizerFactory.CreateMultiple
    (
        [
            typeof(ContentServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]
    );

    public async Task<AnalysisContent> CreateAsync(
        Guid customerId, ProductTypes productType,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        string correlationId = null)
        => await CreateAsync(id: Guid.CreateVersion7(),
            customerId: customerId,
            productType: productType,
            analysisDate: analysisDate,
            operationStatus: operationStatus,
            correlationId: correlationId);

    public async Task<AnalysisContent> CreateAsync(
        Guid id,
        Guid customerId, ProductTypes productType,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        // Create draft
        var draft = new AnalysisContent(
            id: id,
            customerId: customerId,
            productType: productType,
            analysisDate: analysisDate,
            operationStatus: operationStatus,
            correlationId: correlationId
        );

        //Domain Rules
        // Rule01
        // Rule02
        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task SetAnalysisContentNormalizedReferenceAsync(Guid id, Guid normalizedRequestId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.NormalizedRequestId, normalizedRequestId)
        );

        if (affectedCount < 1)
        {
            throw new AnalysisContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<AnalysisContent> SetAnalysisContentNormalizedResultAsync(Guid id, bool isNormalizedSuccess, Guid normalizedRequestId)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new AnalysisContentNotFoundException(L, id.ToString());
        }

        if (oldEntity.OperationStatus != AnalysisContentOperationStates.CreatedWaitForNormalize)
        {
            throw new AnalysisContentStateException(L, id.ToString());
        }

        oldEntity.NormalizedRequestId = normalizedRequestId;
        if (isNormalizedSuccess)
        {
            oldEntity.OperationStatus = AnalysisContentOperationStates.NormalizedWaitForVideoGeneration;
            oldEntity.OperationStatusDescription = AnalysisContentOperationFacilities.ANALYSIS_CONTENT_NORMALIZED_SUCCESS;
        }
        else
        {
            oldEntity.OperationStatus = AnalysisContentOperationStates.OperationFail;
            oldEntity.OperationStatusDescription = AnalysisContentOperationFacilities.ANALYSIS_CONTENT_NORMALIZED_FAIL;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task SetAnalysisContentVideoReferenceAsync(Guid id, Guid videoRequestId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.VideoRequestId, videoRequestId)
        );

        if (affectedCount < 1)
        {
            throw new AnalysisContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<AnalysisContent> SetAnalysisContentVideoResultAsync(Guid id, bool isGenerateSuccess, Guid videoRequestId, string storageVideoUrl)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new AnalysisContentNotFoundException(L, id.ToString());
        }

        if (oldEntity.OperationStatus != AnalysisContentOperationStates.NormalizedWaitForVideoGeneration)
        {
            throw new AnalysisContentStateException(L, id.ToString());
        }

        oldEntity.VideoRequestId = videoRequestId;
        if (isGenerateSuccess)
        {
            oldEntity.OperationStatus = AnalysisContentOperationStates.OperationSuccess;
            oldEntity.OperationStatusDescription = AnalysisContentOperationFacilities.ANALYSIS_CONTENT_VIDEO_GENERATION_SUCCESS;
            oldEntity.StorageVideoUrl = storageVideoUrl;
        }
        else
        {
            oldEntity.OperationStatus = AnalysisContentOperationStates.OperationFail;
            oldEntity.OperationStatusDescription = AnalysisContentOperationFacilities.ANALYSIS_CONTENT_VIDEO_GENERATION_FAIL;
            oldEntity.StorageVideoUrl = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<AnalysisContent> SetAnalysisContentStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new AnalysisContentNotFoundException(L, id.ToString());
        }

        oldEntity.OperationStatus = AnalysisContentOperationStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }
}