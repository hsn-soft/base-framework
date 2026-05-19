using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Exceptions;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.Localization;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreAppContentRepository : EfCoreGenericRepository<AppContent, Guid>, IAppContentRepository
{
    [NotNull] protected IStringLocalizer L { get; }

    public EfCoreAppContentRepository(IServiceProvider provider, IStringLocalizerFactory stringLocalizerFactory, ContentServiceDbContext dbContext) : base(provider, dbContext)
    {
        // DefaultPropertySelector = new List<Expression<Func<AppContent, object>>> { x => x.Client };

        L = stringLocalizerFactory.CreateMultiple([typeof(ContentServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
    }

    public async Task<AppContent> CreateAsync(
        Guid tenantId,
        Guid clientId,
        string slugKey,
        AppContentOperationStates operationStatus,
        string operationStatusDescription = null,
        Guid? normalizedRequestId = null,
        DateTime? releaseTime = null,
        Guid? videoRequestId = null,
        string storageVideoUrl = null,
        string correlationId = null)
        => await CreateAsync(id: Guid.NewGuid(),
            tenantId: tenantId,
            clientId: clientId,
            slugKey: slugKey,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            normalizedRequestId: normalizedRequestId,
            releaseTime: releaseTime,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl,
            correlationId: correlationId);

    public async Task<AppContent> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        string slugKey,
        AppContentOperationStates operationStatus,
        string operationStatusDescription = null,
        Guid? normalizedRequestId = null,
        DateTime? releaseTime = null,
        Guid? videoRequestId = null,
        string storageVideoUrl = null,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.NewGuid();

        // Create draft
        var draft = new AppContent(
            id: id,
            tenantId: tenantId,
            clientId: clientId,
            slugKey: slugKey,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            normalizedRequestId: normalizedRequestId,
            releaseTime: releaseTime,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl,
            correlationId: correlationId
        );

        //Domain Rules
        await ContentDuplicateControlAsync(draft.ClientId, draft.SlugKey);
        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task SetNormalizedRequestReferenceAsync(Guid id, Guid normalizedRequestId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.NormalizedRequestId, normalizedRequestId)
        );

        if (affectedCount < 1)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<AppContent> SetNormalizedContentResultAsync(Guid id,
        bool isNormalizedSuccess,
        Guid normalizedRequestId,
        DateTime? releaseTime)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }

        if (oldEntity.OperationStatus != AppContentOperationStates.CreatedWaitForNormalize)
        {
            throw new AppContentStateException(L, id.ToString());
        }

        oldEntity.SetNormalizedRequestId(normalizedRequestId);
        if (isNormalizedSuccess)
        {
            oldEntity.OperationStatus = AppContentOperationStates.NormalizedWaitForVideoGenerationApprove;
            oldEntity.OperationStatusDescription = AppContentOperationFacilities.APP_CONTENT_NORMALIZED_SUCCESS;
            oldEntity.SetReleaseTime(releaseTime);
        }
        else
        {
            oldEntity.OperationStatus = AppContentOperationStates.OperationFail;
            oldEntity.OperationStatusDescription = AppContentOperationFacilities.APP_CONTENT_NORMALIZED_FAIL;
            oldEntity.SetReleaseTime(null);
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task SetVideoGenerationApprovedAsync(Guid id)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.OperationStatus, AppContentOperationStates.VideoGenerationApprovedWaitForGenerationResults)
            .SetProperty(a => a.OperationStatusDescription, AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_APPROVED)
        );

        if (affectedCount < 1)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }
    }

    public async Task SetVideoGenerationRejectedAsync(Guid id, string rejectReason)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.OperationStatus, AppContentOperationStates.VideoGenerationRejectedReturnAnalysisVideo)
            .SetProperty(a => a.OperationStatusDescription, rejectReason)
        );

        if (affectedCount < 1)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }
    }

    public async Task SetVideoRequestReferenceAsync(Guid id, Guid videoRequestId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.VideoRequestId, videoRequestId)
        );

        if (affectedCount < 1)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<AppContent> SetVideoGenerationResultAsync(Guid id,
        bool isGenerateSuccess,
        Guid videoRequestId,
        string storageVideoUrl)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }

        if (oldEntity.OperationStatus != AppContentOperationStates.VideoGenerationApprovedWaitForGenerationResults)
        {
            throw new AppContentStateException(L, id.ToString());
        }

        oldEntity.SetVideoRequestId(videoRequestId);
        if (isGenerateSuccess)
        {
            oldEntity.OperationStatus = AppContentOperationStates.OperationSuccess;
            oldEntity.OperationStatusDescription = AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_SUCCESS;
            oldEntity.StorageVideoUrl = storageVideoUrl;
        }
        else
        {
            oldEntity.OperationStatus = AppContentOperationStates.OperationFail;
            oldEntity.OperationStatusDescription = AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_FAIL;
            oldEntity.StorageVideoUrl = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<AppContent> SetStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }

        oldEntity.OperationStatus = AppContentOperationStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task RemoveAsync(Guid id)
    {
        var entity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (entity == null)
        {
            throw new AppContentNotFoundException(L, id.ToString());
        }

        //Domain Rule -> Content Dependency Control for Delete
        // Rule01

        string guidGenerated = Guid.NewGuid().ToString("N").ToUpper();
        string uniqueField = guidGenerated + "_" + entity.SlugKey;
        if (uniqueField.Length > AppContentConsts.SlugKeyMaxLength)
        {
            uniqueField = uniqueField[..AppContentConsts.SlugKeyMaxLength];
        }

        entity.SetSlugKey(uniqueField);
        entity.IsDeleted = true;
        await UpdateAsync(entity);
    }

    public async Task<List<Guid>> GetClientDailyTrendContentIdsAsync(Guid clientId, ushort dailyTrendVideoWaitStatisticHour, CancellationToken cancellationToken = default)
    {
        var statisticMinTime = DateTime.UtcNow.AddHours(-1 * dailyTrendVideoWaitStatisticHour);
        var releaseMinDate = DateTime.UtcNow.Date;
        var releaseMaxDate = DateTime.UtcNow.Date.AddDays(1);
        return await GetDbSet().Where(x =>
            x.ClientId == clientId
            && x.OperationStatus == AppContentOperationStates.VideoGenerationRejectedReturnAnalysisVideo
            && x.CreationTime < statisticMinTime // min one day waited on system
            && x.ReleaseTime != null && x.ReleaseTime < releaseMaxDate && x.ReleaseTime >= releaseMinDate).Select(x => x.Id).ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetClientDailyAnalysisContentIdsAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var releaseMinDate = DateTime.UtcNow.Date;
        var releaseMaxDate = DateTime.UtcNow.Date.AddDays(1);
        return await GetDbSet().Where(x =>
            x.ClientId == clientId
            && x.OperationStatus != AppContentOperationStates.CreatedWaitForNormalize
            && x.OperationStatus != AppContentOperationStates.OperationFail
            && x.ReleaseTime != null && x.ReleaseTime < releaseMaxDate && x.ReleaseTime >= releaseMinDate).Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private async Task ContentDuplicateControlAsync(Guid clientId, [NotNull] string slugKey)
    {
        var old = await GetSingleOrDefaultAsync(x => x.Id == clientId && x.SlugKey == slugKey);
        if (old != null)
        {
            throw new AppContentDuplicateException(L, old.Id.ToString()).WithData(nameof(slugKey), slugKey);
        }
    }
}