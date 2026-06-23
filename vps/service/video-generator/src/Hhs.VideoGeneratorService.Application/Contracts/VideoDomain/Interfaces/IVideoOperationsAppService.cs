using Hhs.Shared.Contracts.Events.VideoGenerator;
using Hhs.VideoGeneratorService.Application.Contracts.Events;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.VideoDomain.Interfaces;

public interface IVideoOperationsAppService : IEventApplicationService
{
    Task VideoRequestCreateAsync(VideoGenerationStartedEto input, [CanBeNull] string correlationId = null);
    Task AudioSendAsync(Guid videoRequestId);
    Task AudioFileUploadToStorageAsync(Guid videoRequestId);
    Task VideoRequestSendAsync(Guid videoRequestId);
    Task VideoFileDownloadToLocalAsync(Guid videoRequestId);
    Task VideoFileUploadToStorageAsync(Guid videoRequestId);
    Task SetStatusToFailedAsync(Guid videoRequestId, string failedReason, [CanBeNull] string correlationId = null);

    Task VideoRequestQueryAsync(VideoRequestQueryEto input, [CanBeNull] string correlationId = null);
}