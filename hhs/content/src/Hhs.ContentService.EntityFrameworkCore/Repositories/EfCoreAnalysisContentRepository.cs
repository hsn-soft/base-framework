using System.Globalization;
using System.Linq.Dynamic.Core;
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

public sealed class EfCoreAnalysisContentRepository : EfCoreGenericRepository<AnalysisContent, Guid>, IAnalysisContentRepository
{
    [NotNull] protected IStringLocalizer L { get; }

    public EfCoreAnalysisContentRepository(IServiceProvider provider, IStringLocalizerFactory stringLocalizerFactory, ContentServiceDbContext dbContext) : base(provider, dbContext)
    {
        // DefaultPropertySelector = new List<Expression<Func<AnalysisContent, object>>> { x => x.Client };

        L = stringLocalizerFactory.CreateMultiple([typeof(ContentServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
    }

    public async Task<List<AnalysisContent>> GetPagedListWithFiltersAsync(Guid? clientId = null, DateTime? analysisStartDate = null, DateTime? analysisEndDate = null, AnalysisContentOperationStates? status = null, string sorting = null,
        int maxResultCount = int.MaxValue, int skipCount = 0, CancellationToken cancellationToken = default)
    {
        var queryable = GetQueryable()
            .Include(x => x.Client);

        var query = ApplyFilter(queryable,
            clientId: clientId,
            analysisStartDate: analysisStartDate,
            analysisEndDate: analysisEndDate,
            status: status,
            searchText: null);

        return await query
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? AnalysisContentConsts.GetDefaultSorting() : sorting)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetCountWithFiltersAsync(Guid? clientId = null, DateTime? analysisStartDate = null, DateTime? analysisEndDate = null, AnalysisContentOperationStates? status = null, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(GetQueryable(),
            clientId: clientId,
            analysisStartDate: analysisStartDate,
            analysisEndDate: analysisEndDate,
            status: status,
            searchText: null);

        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<AnalysisContent>> GetFilterListAsync(Guid? clientId = null, DateTime? analysisStartDate = null, DateTime? analysisEndDate = null, AnalysisContentOperationStates? status = null, string sorting = null,
        CancellationToken cancellationToken = default)
    {
        var queryable = GetQueryable()
            .Include(x => x.Client);

        var query = ApplyFilter(queryable,
            clientId: clientId,
            analysisStartDate: analysisStartDate,
            analysisEndDate: analysisEndDate,
            status: status,
            searchText: null);

        return await query
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? AnalysisContentConsts.GetDefaultSorting() : sorting)
            .ToListAsync(cancellationToken);
    }

    public async Task<AnalysisContent> CreateAsync(
        Guid tenantId,
        Guid clientId,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        string operationStatusDescription = null,
        Guid? normalizedAnalysisId = null,
        Guid? videoRequestId = null,
        string storageVideoUrl = null,
        string correlationId = null)
        => await CreateAsync(id: Guid.NewGuid(),
            tenantId: tenantId,
            clientId: clientId,
            analysisDate: analysisDate,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            normalizedAnalysisId: normalizedAnalysisId,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl,
            correlationId: correlationId);

    public async Task<AnalysisContent> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        DateTime analysisDate,
        AnalysisContentOperationStates operationStatus,
        string operationStatusDescription = null,
        Guid? normalizedAnalysisId = null,
        Guid? videoRequestId = null,
        string storageVideoUrl = null,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.NewGuid();

        // Create draft
        var draft = new AnalysisContent(
            id: id,
            tenantId: tenantId,
            clientId: clientId,
            analysisDate: analysisDate,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            normalizedAnalysisId: normalizedAnalysisId,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl,
            correlationId: correlationId
        );

        //Domain Rules
        // Rule01
        // Rule02
        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task SetNormalizedAnalysisReferenceAsync(Guid id, Guid normalizedAnalysisId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.NormalizedAnalysisId, normalizedAnalysisId)
        );

        if (affectedCount < 1)
        {
            throw new AnalysisContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<AnalysisContent> SetNormalizedContentResultAsync(Guid id, bool isNormalizedSuccess, Guid normalizedAnalysisId)
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

        oldEntity.SetNormalizedAnalysisId(normalizedAnalysisId);
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

    public async Task SetVideoRequestReferenceAsync(Guid id, Guid videoRequestId)
    {
        int affectedCount = await GetDbSet().Where(b => b.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.VideoRequestId, videoRequestId)
        );

        if (affectedCount < 1)
        {
            throw new AnalysisContentNotFoundException(L, id.ToString());
        }
    }

    public async Task<AnalysisContent> SetVideoGenerationResultAsync(Guid id, bool isGenerateSuccess, Guid videoRequestId, string storageVideoUrl)
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

        oldEntity.SetVideoRequestId(videoRequestId);
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

    public async Task<AnalysisContent> SetStatusToFailedAsync(Guid id, string failedReason)
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

    private IQueryable<AnalysisContent> ApplyFilter(
        IQueryable<AnalysisContent> query,
        [CanBeNull] string searchText = null,
        Guid? clientId = null,
        DateTime? analysisStartDate = null,
        DateTime? analysisEndDate = null,
        AnalysisContentOperationStates? status = null
    )
    {
        searchText = searchText?.ToLower(new CultureInfo("en-US"));

        // check start and end date value
        if (analysisStartDate != null && analysisEndDate != null && analysisStartDate.Value.Date > analysisEndDate.Value.Date)
        {
            var tmp = analysisStartDate.Value;
            analysisStartDate = analysisEndDate.Value;
            analysisEndDate = tmp;
        }

        if (analysisEndDate != null)
        {
            // limit end date
            query = query.Where(x => x.AnalysisDate < analysisEndDate.Value.AddDays(1).Date);
        }
        else if (analysisStartDate != null)
        {
            // limit start date
            query = query.Where(x => x.AnalysisDate >= analysisStartDate.Value.Date);
        }

        return query
            .WhereIf(!string.IsNullOrWhiteSpace(searchText), e => e.CorrelationId.Contains(searchText))
            .WhereIf(clientId.HasValue, e => e.ClientId == clientId.Value)
            .WhereIf(status.HasValue, e => e.OperationStatus == status.Value);
    }
}