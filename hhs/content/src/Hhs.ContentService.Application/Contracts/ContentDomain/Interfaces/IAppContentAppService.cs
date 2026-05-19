using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public interface IAppContentAppService : IEventApplicationService
{
    Task SetNormalizedRequestReferenceAsync(Guid appContentId, Guid normalizedRequestId);

    Task SetNormalizedResultAsync(Guid appContentId, Guid normalizedRequestId, bool isNormalizedSuccess, DateTime? releaseTime = null, [CanBeNull] string correlationId = null);

    Task SetVideoGenerationRequestReferenceAsync(Guid appContentId, Guid videoRequestId);

    Task SetVideoGenerationResultAsync(Guid appContentId, Guid videoRequestId, bool isGenerateSuccess, [CanBeNull] string storageVideoUrl = null, [CanBeNull] string correlationId = null);

    Task SetStatusToFailedAsync(Guid appContentId, string failedReason, [CanBeNull] string correlationId = null);

    Task TrendVideoGenerationQueryAsync(TrendVideoGenerationQueryEto input, [CanBeNull] string correlationId = null);

    Task TestQueryRequestedAsync(TestQueryRequestedEto input, [CanBeNull] string correlationId = null);
}