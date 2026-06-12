using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public interface ICustomerContentAppService : IEventApplicationService
{
    Task<CustomerContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<CustomerContentDto>> GetPagedListAsync(GetCustomerContentsPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<CustomerContentDto>> GetFilterListAsync(GetCustomerContentsFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<CustomerContentSearchDto>> GetSearchListAsync(GetCustomerContentsSearch searchInput, CancellationToken cancellationToken = default);

    Task SetCustomerContentNormalizedReferenceAsync(Guid customerContentId, Guid normalizedRequestId);
    Task SetCustomerContentNormalizedResultAsync(Guid customerContentId, Guid normalizedRequestId, bool isNormalizedSuccess, DateTime? releaseTime = null, [CanBeNull] string correlationId = null);

    Task SetCustomerContentVideoReferenceAsync(Guid customerContentId, Guid videoRequestId);
    Task SetCustomerContentVideoResultAsync(Guid customerContentId, Guid videoRequestId, bool isGenerateSuccess, [CanBeNull] string storageVideoUrl = null, [CanBeNull] string correlationId = null);

    Task SetCustomerContentStatusToFailedAsync(Guid customerContentId, string failedReason, [CanBeNull] string correlationId = null);

    Task TrendVideoGenerationQueryAsync(TrendVideoGenerationQueryEto input, [CanBeNull] string correlationId = null);

    Task TestQueryRequestedAsync(TestQueryRequestedEto input, [CanBeNull] string correlationId = null);
}