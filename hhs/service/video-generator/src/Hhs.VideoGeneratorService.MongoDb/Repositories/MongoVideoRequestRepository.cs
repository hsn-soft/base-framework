using Hhs.Shared.Helper.Enums;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.Settings;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Consts;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Exceptions;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Repositories;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoVideoRequestRepository(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext dbContext,
    IOptions<VideoRequestQuerySettings> settings
) : MongoGenericRepository<VideoRequest, Guid>(provider, dbContext), IVideoRequestRepository
{
    private readonly VideoRequestQuerySettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

    public async Task<VideoRequest> CreateAsync(
         string scopeKey,
        string domainName,
        ReferenceContentTypes refContentType,
        Guid refContentId,
        VideoRequestStates operationStatus,
        List<NormalizedContentData> normalizedContentDatas,
        string operationStatusDescription = null,
        string externalVideoTraceId = null,
        string externalVideoUrl = null,
        string storageVideoTraceId = null,
        string storageVideoUrl = null,
        string correlationId = null)
        => await CreateAsync(id: Guid.NewGuid(),
            scopeKey: scopeKey,
            domainName: domainName,
            refContentType: refContentType,
            refContentId: refContentId,
            operationStatus: operationStatus,
            normalizedContentDatas: normalizedContentDatas,
            operationStatusDescription: operationStatusDescription,
            externalVideoTraceId: externalVideoTraceId,
            externalVideoUrl: externalVideoUrl,
            storageVideoTraceId: storageVideoTraceId,
            storageVideoUrl: storageVideoUrl,
            correlationId: correlationId
        );

    public async Task<VideoRequest> CreateAsync(
        Guid id,
        string scopeKey,
        string domainName,
        ReferenceContentTypes refContentType,
        Guid refContentId,
        VideoRequestStates operationStatus,
        List<NormalizedContentData> normalizedContentDatas,
        string operationStatusDescription = null,
        string externalVideoTraceId = null,
        string externalVideoUrl = null,
        string storageVideoTraceId = null,
        string storageVideoUrl = null,
        string correlationId = null)
    {
        if (id == Guid.Empty) id = Guid.NewGuid();

        var draft = new VideoRequest(
            id: id,
            scopeKey: scopeKey,
            domainName: domainName,
            refContentType: refContentType,
            refContentId: refContentId,
            operationStatus: operationStatus,
            normalizedContentDatas: normalizedContentDatas,
            operationStatusDescription: operationStatusDescription,
            externalVideoTraceId: externalVideoTraceId,
            externalVideoUrl: externalVideoUrl,
            storageVideoTraceId: storageVideoTraceId,
            storageVideoUrl: storageVideoUrl,
            correlationId: correlationId
        );

        //Domain Rules
        // Rule01
        // Rule02
        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task SetVideoRequestSendResultAsync(Guid id, bool isSendSuccess, string errorMessage, string externalVideoTraceId)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        // if (oldEntity.OperationStatus != VideoRequestStates.CreatedWaitForVideoSent)
        // {
        //     throw new VideoRequestStateException(id.ToString());
        // }

        if (isSendSuccess)
        {
            oldEntity.OperationStatus = VideoRequestStates.VideoSentWaitForVideoGeneration;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_REQUEST_SEND_SUCCESS;
            oldEntity.SetExternalVideoTraceId(externalVideoTraceId);
        }
        else
        {
            oldEntity.OperationStatus = VideoRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_REQUEST_SEND_FAIL + " " + errorMessage;
            oldEntity.SetExternalVideoTraceId(null);
        }

        //Domain Rules
        // Rule01
        // Rule02

        await UpdateAsync(oldEntity);
    }

    public async Task SetAudioRequestSendResultAsync(Guid id, bool isSendSuccess, string errorMessage, List<string> audioFileNames)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        if (isSendSuccess)
        {
            oldEntity.OperationStatus = VideoRequestStates.AudioDownloadedWaitForAudioStorageUpload;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.AUDIO_REQUEST_CREATED;
            oldEntity.AudioFileNames = audioFileNames;
        }
        else
        {
            oldEntity.OperationStatus = VideoRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.AUDIO_REQUEST_SEND_FAIL + " " + errorMessage;
            oldEntity.AudioFileNames = null;
        }

        //Domain Rules
        // Rule01
        // Rule02

        await UpdateAsync(oldEntity);
    }

    public async Task SetAudioGenerationResultAsync(Guid id)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        oldEntity.OperationStatus = VideoRequestStates.AudioSentWaitForAudioGeneration;
        oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.AUDIO_REQUEST_IS_GENERATING;
        oldEntity.AudioFileNames = null;

        //Domain Rules
        // Rule01
        // Rule02

        await UpdateAsync(oldEntity);
    }

    public async Task<VideoRequest> SetAudioFileStorageUploadResultAsync(Guid id, bool isUploadSuccess, string errorMessage, List<string> audioTraceIds,
        List<string> audioUrls)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        /*
        if (oldEntity.OperationStatus != VideoRequestStates.VideoDownloadedWaitForVideoStorageUpload)
        {
            throw new VideoRequestStateException(id.ToString());
        }
        */

        try
        {
            foreach (var audioFileName in oldEntity.AudioFileNames)
                File.Delete(audioFileName);
        }
        catch (Exception)
        {
            // ignored
        }

        if (isUploadSuccess)
        {
            oldEntity.OperationStatus = VideoRequestStates.CreatedWaitForVideoSent;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.AUDIO_FILE_STORAGE_UPLOAD_SUCCESS;
            //oldEntity.AudioTraceId = audioTraceId; //Do we need to store TraceId if we use Worker for Audio file

            oldEntity.AudioFileNames = audioUrls;
        }
        else
        {
            oldEntity.OperationStatus = VideoRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.AUDIO_FILE_STORAGE_UPLOAD_FAIL + " " + errorMessage;
            //oldEntity.SetAudioTraceId(audioTraceId);
            oldEntity.AudioFileNames = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<VideoRequest> SetVideoRequestQueryResultAsync(Guid id,
        bool isQuerySuccess,
        string errorMessage,
        int queryCount,
        DateTime lastQueryTime,
        bool isVideoReady,
        string externalVideoUrl,
        bool isStorageReady,
        string storageVideoUrl
    )
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != VideoRequestStates.VideoSentWaitForVideoGeneration)
        {
            throw new VideoRequestStateException(id.ToString());
        }

        oldEntity.QueryCount = queryCount;
        oldEntity.SetLastQueryTime(lastQueryTime);
        if (isQuerySuccess)
        {
            if (isVideoReady && !string.IsNullOrWhiteSpace(externalVideoUrl))
            {
                oldEntity.ExternalVideoUrl = externalVideoUrl;
                if (isStorageReady)
                {
                    oldEntity.OperationStatus = VideoRequestStates.OperationSuccess;
                    oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_SUCCESS;
                    oldEntity.StorageVideoUrl = storageVideoUrl;
                }
                else
                {
                    oldEntity.OperationStatus = VideoRequestStates.VideoGeneratedWaitForVideoDownload;
                    oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_REQUEST_QUERY_GENERATION_SUCCESS;
                }
            }
            else // video not ready
            {
                if (queryCount >= _settings.ReQueryLimit)
                {
                    oldEntity.OperationStatus = VideoRequestStates.QueryFail;
                    oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_REQUEST_QUERY_GENERATION_FAIL + " " + errorMessage;
                }
                else
                {
                    oldEntity.ExternalVideoUrl = null;
                    oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_REQUEST_IS_GENERATING;
                }
            }
        }
        else
        {
            if (queryCount >= _settings.ReQueryLimit)
            {
                oldEntity.OperationStatus = VideoRequestStates.QueryFail;
                oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_REQUEST_QUERY_GENERATION_FAIL + " " + errorMessage;
            }
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<VideoRequest> SetVideoFileDownloadResultAsync(Guid id, bool isDownloadSuccess, string errorMessage, string localVideoPath)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != VideoRequestStates.VideoGeneratedWaitForVideoDownload)
        {
            throw new VideoRequestStateException(id.ToString());
        }

        if (isDownloadSuccess)
        {
            oldEntity.OperationStatus = VideoRequestStates.VideoDownloadedWaitForVideoStorageUpload;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_FILE_DOWNLOAD_SUCCESS;
            oldEntity.LocalVideoPath = localVideoPath;
        }
        else
        {
            oldEntity.OperationStatus = VideoRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_FILE_DOWNLOAD_FAIL + " " + errorMessage;
            oldEntity.LocalVideoPath = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<VideoRequest> SetVideoFileStorageUploadResultAsync(Guid id, bool isUploadSuccess, string errorMessage, string storageVideoTraceId, string storageVideoUrl)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus != VideoRequestStates.VideoDownloadedWaitForVideoStorageUpload)
        {
            throw new VideoRequestStateException(id.ToString());
        }

        if (isUploadSuccess)
        {
            oldEntity.OperationStatus = VideoRequestStates.OperationSuccess;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_SUCCESS;
            oldEntity.SetStorageVideoTraceId(storageVideoTraceId);

            try
            {
                if (!string.IsNullOrWhiteSpace(oldEntity.LocalVideoPath))
                    File.Delete(oldEntity.LocalVideoPath);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                oldEntity.LocalVideoPath = null;
            }

            oldEntity.StorageVideoUrl = storageVideoUrl;
        }
        else
        {
            oldEntity.OperationStatus = VideoRequestStates.OperationFail;
            oldEntity.OperationStatusDescription = VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_FAIL + " " + errorMessage;
            oldEntity.SetStorageVideoTraceId(storageVideoTraceId);
            oldEntity.StorageVideoUrl = null;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task<VideoRequest> SetStatusToFailedAsync(Guid id, string failedReason)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new VideoRequestNotFoundException(id.ToString());
        }

        oldEntity.OperationStatus = VideoRequestStates.OperationFail;
        oldEntity.OperationStatusDescription = failedReason;
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }
}