using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public interface IAppContentAppService : IEventApplicationService
{
    Task<AppContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<AppContentDto>> GetPagedListAsync(GetAppContentsPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<AppContentDto>> GetFilterListAsync(GetAppContentsFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<AppContentSearchDto>> GetSearchListAsync(GetAppContentsSearch searchInput, CancellationToken cancellationToken = default);

    Task SetNormalizedRequestReferenceAsync(Guid appContentId, Guid normalizedRequestId);

    Task SetNormalizedResultAsync(Guid appContentId, Guid normalizedRequestId, bool isNormalizedSuccess, DateTime? releaseTime = null, [CanBeNull] string correlationId = null);

    Task SetVideoGenerationRequestReferenceAsync(Guid appContentId, Guid videoRequestId);

    Task SetVideoGenerationResultAsync(Guid appContentId, Guid videoRequestId, bool isGenerateSuccess, [CanBeNull] string storageVideoUrl = null, [CanBeNull] string correlationId = null);

    Task SetStatusToFailedAsync(Guid appContentId, string failedReason, [CanBeNull] string correlationId = null);

    Task TrendVideoGenerationQueryAsync(TrendVideoGenerationQueryEto input, [CanBeNull] string correlationId = null);

    Task TestQueryRequestedAsync(TestQueryRequestedEto input, [CanBeNull] string correlationId = null);
}