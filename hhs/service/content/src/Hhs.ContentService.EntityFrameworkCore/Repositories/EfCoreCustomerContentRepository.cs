using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Consts.Facilities;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Exceptions;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.Localization;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreCustomerContentRepository(
    IServiceProvider provider,
    IStringLocalizerFactory stringLocalizerFactory,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<CustomerContent, Guid>(provider, dbContext), ICustomerContentRepository
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

    public async Task<CustomerContent> CreateAsync(
        Guid customerId, ProductTypes productType,
        string slugKey,
        CustomerContentOperationStates operationStatus,
        string correlationId = null)
        => await CreateAsync(id: Guid.CreateVersion7(),
            customerId: customerId,
            productType: productType,
            slugKey: slugKey,
            operationStatus: operationStatus,
            correlationId: correlationId);

    public async Task<CustomerContent> CreateAsync(
        Guid id,
        Guid customerId, ProductTypes productType,
        string slugKey,
        CustomerContentOperationStates operationStatus,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        // Create draft
        var draft = new CustomerContent(
            id: id,
            customerId: customerId,
            productType: productType,
            slugKey: slugKey,
            operationStatus: operationStatus,
            correlationId: correlationId
        );

        //Domain Rules
        await ContentDuplicateControlAsync(scopeKey: draft.ScopeKey, draft.SlugKey);
        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task SetCustomerContentNormalizedReferenceAsync(Guid id, Guid normalizedRequestId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.NormalizedRequestId, normalizedRequestId)
        );

        if (affectedCount < 1)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<CustomerContent> SetCustomerContentNormalizedResultAsync(Guid id,
        bool isNormalizedSuccess,
        Guid normalizedRequestId,
        DateTime? releaseTime)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }

        if (oldEntity.OperationStatus != CustomerContentOperationStates.CreatedWaitForNormalize)
        {
            throw new CustomerContentStateException(L, id.ToString());
        }

        oldEntity.NormalizedRequestId = normalizedRequestId;
        if (isNormalizedSuccess)
        {
            oldEntity.OperationStatus = CustomerContentOperationStates.NormalizedWaitForVideoGenerationApprove;
            oldEntity.OperationStatusDescription = CustomerContentOperationFacilities.CUSTOMER_CONTENT_NORMALIZED_SUCCESS;
            oldEntity.SetReleaseTime(releaseTime);
        }
        else
        {
            oldEntity.OperationStatus = CustomerContentOperationStates.OperationFail;
            oldEntity.OperationStatusDescription = CustomerContentOperationFacilities.CUSTOMER_CONTENT_NORMALIZED_FAIL;
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
            .SetProperty(a => a.OperationStatus, CustomerContentOperationStates.VideoGenerationApprovedWaitForGenerationResults)
            .SetProperty(a => a.OperationStatusDescription, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED)
        );

        if (affectedCount < 1)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }
    }

    public async Task SetVideoGenerationRejectedAsync(Guid id, string rejectReason)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.OperationStatus, CustomerContentOperationStates.VideoGenerationRejectedReturnAnalysisVideo)
            .SetProperty(a => a.OperationStatusDescription, rejectReason)
        );

        if (affectedCount < 1)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }
    }

    public async Task SetCustomerContentVideoReferenceAsync(Guid id, Guid videoRequestId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.VideoRequestId, videoRequestId)
        );

        if (affectedCount < 1)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<CustomerContent> SetCustomerContentVideoResultAsync(Guid id,
        bool isGenerateSuccess,
        Guid videoRequestId,
        string storageVideoUrl)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }

        if (oldEntity.OperationStatus != CustomerContentOperationStates.VideoGenerationApprovedWaitForGenerationResults)
        {
            throw new CustomerContentStateException(L, id.ToString());
        }

        oldEntity.VideoRequestId = videoRequestId;
        if (isGenerateSuccess)
        {
            oldEntity.OperationStatus = CustomerContentOperationStates.OperationSuccess;
            oldEntity.OperationStatusDescription = CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SUCCESS;
            oldEntity.StorageVideoUrl = storageVideoUrl;
        }
        else
        {
            oldEntity.OperationStatus = CustomerContentOperationStates.OperationFail;
            oldEntity.OperationStatusDescription = CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_FAIL;
            oldEntity.StorageVideoUrl = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<CustomerContent> SetCustomerContentStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldEntity == null)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }

        oldEntity.OperationStatus = CustomerContentOperationStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task RemoveAsync(Guid id)
    {
        var entity = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (entity == null)
        {
            throw new CustomerContentNotFoundException(L, id.ToString());
        }

        //Domain Rule -> Content Dependency Control for Delete
        // Rule01

        string guidGenerated = Guid.CreateVersion7().ToString("N").ToUpper();
        string uniqueField = guidGenerated + "_" + entity.SlugKey;
        if (uniqueField.Length > CustomerContentConsts.SlugKeyMaxLength)
        {
            uniqueField = uniqueField[..CustomerContentConsts.SlugKeyMaxLength];
        }

        entity.SetSlugKey(uniqueField);
        entity.IsDeleted = true;
        await UpdateAsync(entity);
    }

    public async Task<List<Guid>> GetCustomerDailyTrendContentIdsAsync(string scopeKey, ushort dailyTrendVideoWaitStatisticHour, CancellationToken cancellationToken = default)
    {
        var statisticMinTime = DateTime.UtcNow.AddHours(-1 * dailyTrendVideoWaitStatisticHour);
        var releaseMinDate = DateTime.UtcNow.Date;
        var releaseMaxDate = DateTime.UtcNow.Date.AddDays(1);
        return await GetDbSet().Where(x =>
            x.ScopeKey == scopeKey
            && x.OperationStatus == CustomerContentOperationStates.VideoGenerationRejectedReturnAnalysisVideo
            && x.CreationTime < statisticMinTime // min one day waited on system
            && x.ReleaseTime != null && x.ReleaseTime < releaseMaxDate && x.ReleaseTime >= releaseMinDate).Select(x => x.Id).ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetCustomerDailyAnalysisContentIdsAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var releaseMinDate = DateTime.UtcNow.Date;
        var releaseMaxDate = DateTime.UtcNow.Date.AddDays(1);
        return await GetDbSet().Where(x =>
            x.ScopeKey == scopeKey
            && x.OperationStatus != CustomerContentOperationStates.CreatedWaitForNormalize
            && x.OperationStatus != CustomerContentOperationStates.OperationFail
            && x.ReleaseTime != null && x.ReleaseTime < releaseMaxDate && x.ReleaseTime >= releaseMinDate).Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private async Task ContentDuplicateControlAsync([NotNull] string scopeKey, [NotNull] string slugKey)
    {
        var old = await GetSingleOrDefaultAsync(x => x.ScopeKey == scopeKey && x.SlugKey == slugKey);
        if (old != null)
        {
            throw new CustomerContentDuplicateException(L, old.Id.ToString())
                .WithData(nameof(scopeKey), scopeKey)
                .WithData(nameof(slugKey), slugKey);
        }
    }
}