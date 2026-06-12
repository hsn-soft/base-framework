using Hhs.Shared.Helper.Enums;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.VideoDomain.Repositories;

public interface IVideoRequestRepository : IReadOnlyGenericRepository<VideoRequest, Guid>
{
    Task<VideoRequest> CreateAsync(
        Guid tenantId,
        Guid clientId,
        [NotNull] string domainName,
        ReferenceContentTypes refContentType,
        Guid refContentId,
        VideoRequestStates operationStatus,
        [NotNull] List<NormalizedContentData> normalizedContentDatas,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string externalVideoTraceId = null,
        [CanBeNull] string externalVideoUrl = null,
        [CanBeNull] string storageVideoTraceId = null,
        [CanBeNull] string storageVideoUrl = null,
        [CanBeNull] string correlationId = null);

    Task<VideoRequest> CreateAsync(
        Guid id,
        Guid tenantId,
        Guid clientId,
        [NotNull] string domainName,
        ReferenceContentTypes refContentType,
        Guid refContentId,
        VideoRequestStates operationStatus,
        [NotNull] List<NormalizedContentData> normalizedContentDatas,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string externalVideoTraceId = null,
        [CanBeNull] string externalVideoUrl = null,
        [CanBeNull] string storageVideoTraceId = null,
        [CanBeNull] string storageVideoUrl = null,
        [CanBeNull] string correlationId = null);

    Task SetVideoRequestSendResultAsync(Guid id, bool isSendSuccess, [CanBeNull] string errorMessage, [CanBeNull] string externalVideoTraceId);

    Task SetAudioRequestSendResultAsync(Guid id, bool isSendSuccess, [CanBeNull] string errorMessage, [CanBeNull] List<string> audioFileNames);

    Task SetAudioGenerationResultAsync(Guid id);

    Task<VideoRequest> SetAudioFileStorageUploadResultAsync(Guid id, bool isUploadSuccess, [CanBeNull] string errorMessage, [CanBeNull] List<string> audioTraceIds, [CanBeNull] List<string> audioUrls);

    Task<VideoRequest> SetVideoRequestQueryResultAsync(Guid id,
        bool isQuerySuccess,
        [CanBeNull] string errorMessage,
        int queryCount,
        DateTime lastQueryTime,
        bool isVideoReady,
        string externalVideoUrl,
        bool isStorageReady,
        string storageVideoUrl);

    Task<VideoRequest> SetVideoFileDownloadResultAsync(Guid id, bool isDownloadSuccess, [CanBeNull] string errorMessage, [CanBeNull] string localVideoPath);

    Task<VideoRequest> SetVideoFileStorageUploadResultAsync(Guid id, bool isUploadSuccess, [CanBeNull] string errorMessage, [CanBeNull] string storageVideoTraceId, [CanBeNull] string storageVideoUrl);

    Task<VideoRequest> SetStatusToFailedAsync(Guid id, [CanBeNull] string failedReason);

    Task<VideoRequest> FindByUniqueKeysAsync(Guid clientId, Guid refContentId, CancellationToken cancellationToken = default);
}