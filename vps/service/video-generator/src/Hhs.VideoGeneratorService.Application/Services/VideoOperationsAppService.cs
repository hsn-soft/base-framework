using System.Net;
using Hhs.Shared.Contracts.Events.Content;
using Hhs.Shared.Contracts.Events.VideoGenerator;
using Hhs.Shared.Helper.Enums;
using Hhs.VideoGeneratorService.Application.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Audio.ElevenLabs;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Creatomoate;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Yepic;
using Hhs.VideoGeneratorService.Application.Contracts.VideoDomain.Interfaces;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.Settings;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Consts;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Repositories;
using HsnSoft.Base;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoOperationsAppService(
    IServiceProvider provider,
    IColossyanAiVideoGenerationProvider colossyanAiVideoGenerationProvider,
    IOptions<ColossyanAiSettings> colossyanAiSettings,
    IYepicAiVideoGenerationProvider yepicAiVideoGenerationProvider,
    IOptions<YepicAiSettings> yepicAiSettings,
    IDidAiVideoGenerationProvider didAiVideoGenerationProvider,
    IHeyGenVideoGenerationProvider heyGenVideoGenerationProvider,
    ICreatomateVideoGenerationProvider creatomateVideoGenerationProvider,
    IOptions<DidAiSettings> didAiSettings,
    IOptions<HeyGenSettings> heyGenSettings,
    IOptions<CreatomateSettings> creatomateSettings,
    IBunnyCdnSelfVideoStorageProvider bunnyCdnSelfVideoStorageProvider,
    IOptions<BunnyCdnSelfStorageSettings> bunnyCdnSelfStorageSettings,
    IBunnyCdnS3VideoStorageProvider bunnyCdnS3VideoStorageProvider,
    IOptions<BunnyCdnS3StorageSettings> bunnyCdnS3StorageSettings,
    IElevenLabsAudioProvider elevenLabsAudioProvider,
    IOptions<ElevenLabsSettings> elevenLabsSettings,
    IOptions<VideoGenerationSettings> serviceSettings,
    IOptions<VideoRequestQuerySettings> querySettings,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IVideoRequestRepository videoRequestRepository
) : ApplicationServiceBase(provider), IVideoOperationsAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    private readonly ColossyanAiSettings _colossyanAiSettings = colossyanAiSettings?.Value ?? throw new ArgumentNullException(nameof(colossyanAiSettings));
    private readonly YepicAiSettings _yepicAiSettings = yepicAiSettings?.Value ?? throw new ArgumentNullException(nameof(yepicAiSettings));
    private readonly DidAiSettings _didAiSettings = didAiSettings?.Value ?? throw new ArgumentNullException(nameof(didAiSettings));
    private readonly HeyGenSettings _heyGenSettings = heyGenSettings?.Value ?? throw new ArgumentNullException(nameof(heyGenSettings));
    private readonly CreatomateSettings _creatomateSettings = creatomateSettings?.Value ?? throw new ArgumentNullException(nameof(creatomateSettings));
    private readonly BunnyCdnSelfStorageSettings _bunnyCdnSelfStorageSettings = bunnyCdnSelfStorageSettings?.Value ?? throw new ArgumentNullException(nameof(bunnyCdnSelfStorageSettings));
    private readonly BunnyCdnS3StorageSettings _bunnyCdnS3StorageSettings = bunnyCdnS3StorageSettings?.Value ?? throw new ArgumentNullException(nameof(bunnyCdnS3StorageSettings));
    private readonly ElevenLabsSettings _elevenLabsSettings = elevenLabsSettings?.Value ?? throw new ArgumentNullException(nameof(elevenLabsSettings));
    private readonly VideoGenerationSettings _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));
    private readonly VideoRequestQuerySettings _querySettings = querySettings?.Value ?? throw new ArgumentNullException(nameof(querySettings));

    public async Task VideoRequestCreateAsync(VideoGenerationStartedEto input, string correlationId = null)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.ScopeKey) || input.ReferenceContentId == Guid.Empty
            || input.EncodedNormalizedContentDatas is { Count: < 1 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await videoRequestRepository.GetSingleOrDefaultAsync(x
            => x.ScopeKey == input.ScopeKey && x.RefContentId == input.ReferenceContentId);

        if (videoRequest != null) return;

        var placedVideoRequest = await videoRequestRepository.CreateAsync(
            scopeKey: input.ScopeKey,
            domainName: input.DomainName,
            refContentType: input.ReferenceContentType,
            refContentId: input.ReferenceContentId,
            operationStatus: VideoRequestStates.CreatedWaitForVideoSent,
            normalizedContentDatas: Mapper.Map<List<EncodedNormalizedContentData>, List<NormalizedContentData>>(input.EncodedNormalizedContentDatas),
            correlationId: correlationId);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"Video request created",
            reference: new
            {
                placedVideoRequest.ScopeKey,
                ClientDomain = placedVideoRequest.DomainName,
                placedVideoRequest.RefContentType,
                placedVideoRequest.RefContentId,
                RefVideoRequestId = placedVideoRequest.Id
            },
            facility: VideoRequestOperationFacilities.VIDEO_REQUEST_CREATED,
            correlationId: correlationId,
            exception: null
        ));

        // Integration Event for ContentService(set reference)
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoGenerationRequestCreatedEto(
                ReferenceContentType: input.ReferenceContentType,
                ReferenceContentId: input.ReferenceContentId,
                RefVideoRequestId: placedVideoRequest.Id
            ));

        var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == placedVideoRequest.ScopeKey);
        if (customerVpSetting is null) throw new ArgumentNullException(nameof(placedVideoRequest.ScopeKey));

        bool isProviderAudioOperationEnabled = customerVpSetting.VideoGenerationProviderType switch
        {
            VideoGenerationProviderTypes.COLOSSYAN_AI => _colossyanAiSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.YEPIC_AI => _yepicAiSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.DID_AI => _didAiSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.HEYGEN_AI => _heyGenSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.CREATOMATE => _creatomateSettings.IsProviderAudioOperationEnabled,
            _ => throw new Exception("Unknown audio generation provider type")
        };

        if (isProviderAudioOperationEnabled && customerVpSetting.IsEnabledExternalAudioGeneration)
        {
            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioSendStartedEto(
                    VideoRequestId: placedVideoRequest.Id
                ));
        }
        else
        {
            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoRequestSendStartedEto(
                    VideoRequestId: placedVideoRequest.Id
                ));
        }
    }

    public async Task AudioSendAsync(Guid videoRequestId)
    {
        if (videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool audioRequestSendOperationSuccess = false;
        var audioFileNames = new List<string>();
        string errorMessage = null;

        var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == videoRequest.ScopeKey);
        if (customerVpSetting is null) throw new ArgumentNullException(nameof(videoRequest.ScopeKey));

        try
        {
            if (videoRequest.NormalizedContentDatas is { Count: < 1 })
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] content data can not be empty", videoRequest.Id.ToString());

                throw new ArgumentNullException(nameof(videoRequest.NormalizedContentDatas));
            }

            int index = 0;
            foreach (var normalizedContentData in videoRequest.NormalizedContentDatas)
            {
                index++;
                try
                {
                    AudioGenerationSendResponseDto response = null;
                    if (!_serviceSettings.SkipAudioGenerationOperation)
                    {
                        /*
                        var audioGenerationSendRequest = new AudioGenerationSendRequestDto
                        {
                            AudioContent = videoRequestItem.NormalizedContentDatas.Select(x => x.NormalizedContent).JoinAsString(". "),
                            VideoRequestReferenceId = videoRequestItem.Id
                        };
                        */
                        var audioGenerationSendRequest = new AudioGenerationSendRequestDto { AudioContent = normalizedContentData.NormalizedContent, VideoRequestReferenceId = videoRequest.Id + "-" + index };

                        response = await elevenLabsAudioProvider.SendAudioAsync(audioGenerationSendRequest,
                            _elevenLabsSettings);
                    }
                    else
                    {
                        response = new AudioGenerationSendResponseDto
                        {
                            HasError = false,
                            ErrorMessage = null,
                            AudioFileName =
                                "https://assets-techsummus.b-cdn.net/7bad1d74-0f52-4946-98af-af626bef2eeb-" + index +
                                ".mp3"
                        };
                    }

                    if (response is { HasError: false })
                    {
                        audioFileNames.Add(response.AudioFileName);
                        audioRequestSendOperationSuccess = !string.IsNullOrWhiteSpace(response.AudioFileName);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: $"Audio request send success [ {index} ]",
                            reference: new
                            {
                                videoRequest.ScopeKey,
                                ClientDomain = videoRequest.DomainName,
                                videoRequest.RefContentType,
                                videoRequest.RefContentId,
                                RefVideoRequestId = videoRequest.Id
                            },
                            facility: VideoRequestOperationFacilities.AUDIO_REQUEST_SEND_SUCCESS,
                            correlationId: videoRequest.CorrelationId,
                            exception: null
                        ));
                    }
                    else throw new Exception(response?.ErrorMessage ?? string.Empty);

                    if (!audioRequestSendOperationSuccess) throw new Exception("Audio File Couldnt be created");
                }
                catch (Exception ex)
                {
                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: ex.Message,
                        reference: new
                        {
                            videoRequest.ScopeKey,
                            ClientDomain = videoRequest.DomainName,
                            videoRequest.RefContentType,
                            videoRequest.RefContentId,
                            RefVideoRequestId = videoRequest.Id
                        },
                        facility: VideoRequestOperationFacilities.AUDIO_REQUEST_SEND_FAIL,
                        correlationId: videoRequest.CorrelationId,
                        exception: ex
                    ));

                    audioRequestSendOperationSuccess = false;
                    errorMessage = ex.Message;
                }

                if (errorMessage != null)
                    break;
            }

            await videoRequestRepository.SetAudioRequestSendResultAsync(
                id: videoRequestId,
                isSendSuccess: audioRequestSendOperationSuccess,
                errorMessage: errorMessage,
                audioFileNames: audioRequestSendOperationSuccess ? audioFileNames : null);

            if (audioRequestSendOperationSuccess)
            {
                if (!_serviceSettings.SkipAudioGenerationOperation)
                {
                    bool isEnabledWaitAudioFileGeneration = customerVpSetting.VideoGenerationProviderType switch
                    {
                        VideoGenerationProviderTypes.COLOSSYAN_AI => _colossyanAiSettings
                            .IsEnabledWaitAudioFileGeneration,
                        VideoGenerationProviderTypes.YEPIC_AI => _yepicAiSettings.IsEnabledWaitAudioFileGeneration,
                        VideoGenerationProviderTypes.DID_AI => _didAiSettings.IsEnabledWaitAudioFileGeneration,
                        VideoGenerationProviderTypes.HEYGEN_AI => _heyGenSettings.IsEnabledWaitAudioFileGeneration,
                        VideoGenerationProviderTypes.CREATOMATE => _creatomateSettings.IsEnabledWaitAudioFileGeneration,
                        _ => throw new Exception("Unknown audio generation provider type")
                    };

                    if (!isEnabledWaitAudioFileGeneration)
                    {
                        bool isEnabledAudioFileDownloadOperation = customerVpSetting.VideoGenerationProviderType switch
                        {
                            VideoGenerationProviderTypes.COLOSSYAN_AI => _colossyanAiSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.YEPIC_AI => _yepicAiSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.DID_AI => _didAiSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.HEYGEN_AI => _heyGenSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.CREATOMATE => _creatomateSettings.IsEnabledAudioFileDownloadOperation,
                            _ => throw new Exception("Unknown audio generation provider type")
                        };

                        if (!isEnabledAudioFileDownloadOperation)
                        {
                            // Integration Event for VideoGeneratorService
                            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                                eventMessage: new AudioFileUploadToStorageStartedEto(
                                    VideoRequestId: videoRequestId
                                ));
                        }
                        else
                        {
                            // // Integration Event for VideoGeneratorService
                            // await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            //     eventMessage: new AudioFileDownloadToLocalStartedEto(
                            //         VideoRequestReferenceId: videoRequestId
                            //     ));
                        }
                    }
                    else
                    {
                        //Set status AudioSentWaitForAudioGeneration for AudioQueryWorker
                        await videoRequestRepository.SetAudioGenerationResultAsync(id: videoRequestId);
                    }
                }
                else
                {
                    // Integration Event for VideoGeneratorService
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new VideoRequestSendStartedEto(
                            VideoRequestId: videoRequestId
                        ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.AUDIO_REQUEST_SEND_FAIL,
                correlationId: videoRequest.CorrelationId,
                exception: ex
            ));

            audioRequestSendOperationSuccess = false;
            errorMessage = ex.Message;
        }
    }

    public async Task AudioFileUploadToStorageAsync(Guid videoRequestId)
    {
        if (videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool audioUploadOperationSuccess = false;
        string errorMessage = null;
        List<string> audioTraceIds = new List<string>();
        List<string> audioUrls = new List<string>();

        var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == videoRequest.ScopeKey);
        if (customerVpSetting is null) throw new ArgumentNullException(nameof(videoRequest.ScopeKey));

        try
        {
            if (videoRequest.AudioFileNames is { Count: < 1 })
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] AudioFileNames can not be empty", videoRequest.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequest.AudioFileNames));
            }

            foreach (var audioFileName in videoRequest.AudioFileNames)
            {
                var audioUploadRequest = new FileStorageUploadRequestDto { LocalFilePath = audioFileName };

                var response = customerVpSetting.VideoGenerationProviderType switch
                {
                    VideoGenerationProviderTypes.COLOSSYAN_AI => await bunnyCdnSelfVideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnSelfStorageSettings,
                        customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.YEPIC_AI => await bunnyCdnSelfVideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnSelfStorageSettings,
                        customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.DID_AI => await bunnyCdnS3VideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnS3StorageSettings,
                        customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.HEYGEN_AI => await bunnyCdnS3VideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnS3StorageSettings,
                        customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.CREATOMATE => await bunnyCdnS3VideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnS3StorageSettings,
                        customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                    _ => throw new Exception("Unknown video generation provider type")
                };

                if (response is { HasError: false })
                {
                    audioTraceIds.Add(response.FileTraceId);
                    audioUrls.Add(response.FileStorageUrl);
                    audioUploadOperationSuccess = !string.IsNullOrWhiteSpace(response.FileStorageUrl);
                }
                else throw new Exception(response?.ErrorMessage ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.AUDIO_FILE_STORAGE_UPLOAD_FAIL,
                correlationId: videoRequest.CorrelationId,
                exception: ex
            ));
            audioUploadOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedVideoRequest = await videoRequestRepository.SetAudioFileStorageUploadResultAsync(
            id: videoRequestId,
            isUploadSuccess: audioUploadOperationSuccess,
            errorMessage: errorMessage,
            audioTraceIds: audioTraceIds,
            audioUrls: audioUrls);

        if (audioUploadOperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Audio file storage upload success",
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.AUDIO_FILE_STORAGE_UPLOAD_SUCCESS,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoRequestSendStartedEto(
                    VideoRequestId: updatedVideoRequest.Id
                ));

            return;
        }

        //  Integration Event for ContentService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoGenerationResultEto(
                ReferenceContentType: updatedVideoRequest.RefContentType,
                ReferenceContentId: updatedVideoRequest.RefContentId,
                IsGenerateSuccess: updatedVideoRequest.OperationStatus == VideoRequestStates.OperationSuccess,
                VideoRequestId: updatedVideoRequest.Id,
                StorageVideoUrl: updatedVideoRequest.StorageVideoUrl
            ));
    }

    public async Task VideoRequestSendAsync(Guid videoRequestId)
    {
        if (videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool videoRequestSendOperationSuccess;
        string errorMessage = null;
        string externalVideoTraceId = null;
        try
        {
            if (videoRequest.NormalizedContentDatas is { Count: < 1 })
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] content data can not be empty", videoRequest.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequest.NormalizedContentDatas));
            }

            var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == videoRequest.ScopeKey);
            if (customerVpSetting is null) throw new ArgumentNullException(nameof(videoRequest.ScopeKey));

            VideoGenerationSendResponseDto response;

            if (!_serviceSettings.SkipVideoGenerationOperation)
            {
                if (customerVpSetting.IsEnabledVideoGeneration)
                {
                    if (videoRequest.RefContentType != ReferenceContentTypes.CUSTOMER_CONTENT && videoRequest.RefContentType != ReferenceContentTypes.ANALYSIS_CONTENT)
                    {
                        _logger.LogError("VideoRequest[{VideoRequestReferenceId}] RefContentType is invalid", videoRequest.Id.ToString());
                        throw new Exception($"VideoRequest[{videoRequest.Id.ToString()}] RefContentType is invalid");
                    }

                    var videoGenerationSendRequest = new VideoGenerationSendRequestDto
                    {
                        VideoRequestId = videoRequest.Id, RefContentType = videoRequest.RefContentType, VideoContentDatas = Mapper.Map<List<NormalizedContentData>, List<VideoContentDataDto>>(videoRequest.NormalizedContentDatas), AudioFileNames = videoRequest.AudioFileNames
                    };
                    response = customerVpSetting.VideoGenerationProviderType switch
                    {
                        VideoGenerationProviderTypes.COLOSSYAN_AI => await colossyanAiVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _colossyanAiSettings.IsProviderSupportPreSignedStorage,
                            (ClientColossyanAiSettings)customerVpSetting.VideoGenerationProviderSettings, _bunnyCdnSelfStorageSettings,
                            customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.YEPIC_AI => await yepicAiVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _yepicAiSettings.IsProviderSupportPreSignedStorage,
                            (ClientYepicAiSettings)customerVpSetting.VideoGenerationProviderSettings, _bunnyCdnSelfStorageSettings,
                            customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.DID_AI => await didAiVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _didAiSettings.IsProviderSupportPreSignedStorage,
                            (ClientDidAiSettings)customerVpSetting.VideoGenerationProviderSettings, _bunnyCdnS3StorageSettings,
                            customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.HEYGEN_AI => await heyGenVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _heyGenSettings.IsProviderSupportPreSignedStorage,
                            (ClientHeyGenSettings)customerVpSetting.VideoGenerationProviderSettings, _bunnyCdnS3StorageSettings,
                            customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.CREATOMATE => await creatomateVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _creatomateSettings.IsProviderSupportPreSignedStorage,
                            (ClientCreatomateSettings)customerVpSetting.VideoGenerationProviderSettings, _bunnyCdnS3StorageSettings,
                            customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        _ => throw new Exception("Unknown video generation provider type")
                    };
                }
                else throw new Exception("VIDEO_GENERATION_DISABLED");
            }
            else
            {
                response = new VideoGenerationSendResponseDto { HasError = false, ErrorMessage = null, ExternalVideoTraceId = "test-id" };
            }

            if (response is { HasError: false })
            {
                externalVideoTraceId = response.ExternalVideoTraceId;
                videoRequestSendOperationSuccess = !string.IsNullOrWhiteSpace(externalVideoTraceId);
            }
            else throw new Exception(response?.ErrorMessage ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_REQUEST_SEND_FAIL,
                correlationId: videoRequest.CorrelationId,
                exception: ex
            ));
            videoRequestSendOperationSuccess = false;
            errorMessage = ex.Message;
        }

        await videoRequestRepository.SetVideoRequestSendResultAsync(
            id: videoRequestId,
            isSendSuccess: videoRequestSendOperationSuccess,
            errorMessage: errorMessage,
            externalVideoTraceId: videoRequestSendOperationSuccess ? externalVideoTraceId : null);

        if (videoRequestSendOperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Video request send success",
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_REQUEST_SEND_SUCCESS,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoRequestQueryEto(
                    VideoRequestId: videoRequest.Id
                ));
        }
    }

    public async Task VideoRequestQueryAsync(VideoRequestQueryEto input, string correlationId = null)
    {
        if (input?.VideoRequestId == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await videoRequestRepository.GetByIdOrDefaultAsync(input.VideoRequestId);
        if (videoRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool videoRequestQueryOperationSuccess;
        string queryErrorMessage = string.Empty;
        bool isVideoReady = false;
        bool isStorageReady = false;
        string externalVideoUrl = null;
        string storageVideoUrl = null;

        try
        {
            if (string.IsNullOrWhiteSpace(videoRequest.ExternalVideoTraceId))
            {
                throw new ArgumentNullException(nameof(videoRequest.ExternalVideoTraceId), "External video trace id can not be empty");
            }

            VideoGenerationQueryResponseDto response;
            if (!_serviceSettings.SkipVideoGenerationOperation)
            {
                var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == videoRequest.ScopeKey);
                if (customerVpSetting is null) throw new ArgumentNullException(nameof(videoRequest.ScopeKey));

                if (!customerVpSetting.IsEnabledVideoGeneration)
                {
                    // block re-query operation
                    videoRequest.QueryCount = _querySettings.ReQueryLimit;
                    throw new Exception("VIDEO_GENERATION_DISABLED");
                }

                var videoGenerationQueryRequest = new VideoGenerationQueryRequestDto { ExternalVideoTraceId = videoRequest.ExternalVideoTraceId };
                switch (customerVpSetting.VideoGenerationProviderType)
                {
                    case VideoGenerationProviderTypes.COLOSSYAN_AI:
                        {
                            response = await colossyanAiVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _colossyanAiSettings.IsProviderSupportPreSignedStorage,
                                (ClientColossyanAiSettings)customerVpSetting.VideoGenerationProviderSettings,
                                _bunnyCdnSelfStorageSettings,
                                customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.YEPIC_AI:
                        {
                            response = await yepicAiVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _yepicAiSettings.IsProviderSupportPreSignedStorage,
                                (ClientYepicAiSettings)customerVpSetting.VideoGenerationProviderSettings,
                                _bunnyCdnSelfStorageSettings,
                                customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.DID_AI:
                        {
                            response = await didAiVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _didAiSettings.IsProviderSupportPreSignedStorage,
                                (ClientDidAiSettings)customerVpSetting.VideoGenerationProviderSettings,
                                _bunnyCdnS3StorageSettings,
                                customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.HEYGEN_AI:
                        {
                            response = await heyGenVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _heyGenSettings.IsProviderSupportPreSignedStorage,
                                (ClientHeyGenSettings)customerVpSetting.VideoGenerationProviderSettings,
                                _bunnyCdnS3StorageSettings,
                                customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.CREATOMATE:
                        {
                            response = await creatomateVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _creatomateSettings.IsProviderSupportPreSignedStorage,
                                (ClientCreatomateSettings)customerVpSetting.VideoGenerationProviderSettings,
                                _bunnyCdnS3StorageSettings,
                                customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName);
                            break;
                        }
                    default: throw new Exception("Unknown video generation provider type");
                }
            }
            else
            {
                response = new VideoGenerationQueryResponseDto
                {
                    HasError = false,
                    ErrorMessage = null,
                    IsVideoReady = true,
                    ExternalVideoUrl = "test_external_video.mp4", // Required entity update operation check
                    StorageVideoUrl = "https://assets-techsummus.b-cdn.net/7bad1d74-0f52-4946-98af-af626bef2eeb.mp4"
                };
            }

            if (response is { HasError: false })
            {
                videoRequestQueryOperationSuccess = true;
                if (response.IsVideoReady)
                {
                    isVideoReady = true;
                    externalVideoUrl = response.ExternalVideoUrl;
                    storageVideoUrl = response.StorageVideoUrl;
                    isStorageReady = !string.IsNullOrWhiteSpace(storageVideoUrl);
                }
            }
            else throw new Exception(response?.ErrorMessage ?? string.Empty);
        }
        catch (Exception e)
        {
            videoRequestQueryOperationSuccess = false;
            queryErrorMessage = e.Message;
        }

        if (isVideoReady)
        {
            var updatedVideoRequest = await videoRequestRepository.SetVideoRequestQueryResultAsync(
                id: videoRequest.Id,
                isQuerySuccess: true,
                errorMessage: null,
                queryCount: videoRequest.QueryCount + 1,
                lastQueryTime: DateTime.UtcNow,
                isVideoReady: isVideoReady,
                externalVideoUrl: externalVideoUrl,
                isStorageReady: isStorageReady,
                storageVideoUrl: storageVideoUrl
            );

            if (updatedVideoRequest.OperationStatus == VideoRequestStates.VideoGeneratedWaitForVideoDownload)
            {
                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: $"Video request query result, video is successfully generated",
                    reference: new
                    {
                        videoRequest.ScopeKey,
                        ClientDomain = videoRequest.DomainName,
                        videoRequest.RefContentType,
                        videoRequest.RefContentId,
                        RefVideoRequestId = videoRequest.Id
                    },
                    facility: VideoRequestOperationFacilities.VIDEO_REQUEST_QUERY_GENERATION_SUCCESS,
                    correlationId: videoRequest.CorrelationId,
                    exception: null
                ));

                // Integration Event for VideoGeneratorService
                await EventBus.PublishAsync(correlationId: updatedVideoRequest.CorrelationId,
                    eventMessage: new VideoFileDownloadToLocalStartedEto(
                        VideoRequestId: updatedVideoRequest.Id
                    ));

                _logger.LogDebug("VideoRequest[{VideoRequestId}] | {QueryResult} | IS READY [ {ExternalVideoUrl} ]",
                    videoRequest.Id.ToString(), "QUERY_RESULT", externalVideoUrl ?? string.Empty);
            }
            else if (updatedVideoRequest.OperationStatus == VideoRequestStates.OperationSuccess)
            {
                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: $"Video file storage upload success by provider",
                    reference: new
                    {
                        updatedVideoRequest.ScopeKey,
                        ClientDomain = videoRequest.DomainName,
                        updatedVideoRequest.RefContentType,
                        updatedVideoRequest.RefContentId,
                        RefVideoRequestId = updatedVideoRequest.Id
                    },
                    facility: VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_SUCCESS,
                    correlationId: updatedVideoRequest.CorrelationId,
                    exception: null
                ));

                //  Integration Event for ContentService
                await EventBus.PublishAsync(correlationId: updatedVideoRequest.CorrelationId,
                    eventMessage: new VideoGenerationResultEto(
                        ReferenceContentType: (ReferenceContentTypes)updatedVideoRequest.RefContentType,
                        ReferenceContentId: updatedVideoRequest.RefContentId,
                        IsGenerateSuccess: updatedVideoRequest.OperationStatus == VideoRequestStates.OperationSuccess,
                        VideoRequestId: updatedVideoRequest.Id,
                        StorageVideoUrl: updatedVideoRequest.StorageVideoUrl
                    ));
            }
        }
        else
        {
            await videoRequestRepository.SetVideoRequestQueryResultAsync(
                id: videoRequest.Id,
                isQuerySuccess: videoRequestQueryOperationSuccess,
                errorMessage: queryErrorMessage,
                queryCount: videoRequest.QueryCount + 1,
                lastQueryTime: DateTime.UtcNow,
                isVideoReady: isVideoReady,
                isStorageReady: isStorageReady,
                externalVideoUrl: externalVideoUrl,
                storageVideoUrl: storageVideoUrl);

            if (videoRequestQueryOperationSuccess)
            {
                _logger.LogDebug("VideoRequest[{VideoRequestId}] | {QueryResult} | {OperationFacility} [{QueryCount}]",
                    videoRequest.Id.ToString(), "QUERY_RESULT",
                    VideoRequestOperationFacilities.VIDEO_REQUEST_IS_GENERATING,
                    (videoRequest.QueryCount + 1).ToString()
                );

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: $"Video request query result, video is generating...",
                    reference: new
                    {
                        videoRequest.ScopeKey,
                        ClientDomain = videoRequest.DomainName,
                        videoRequest.RefContentType,
                        videoRequest.RefContentId,
                        RefVideoRequestId = videoRequest.Id
                    },
                    facility: VideoRequestOperationFacilities.VIDEO_REQUEST_IS_GENERATING,
                    correlationId: videoRequest.CorrelationId,
                    exception: null
                ));
            }
            else
            {
                if (videoRequest.QueryCount + 1 >= _querySettings.ReQueryLimit)
                {
                    _logger.LogError("VideoRequest[{VideoRequestId}] | {QueryResult} | {ErrorMessage}", videoRequest.Id.ToString(), "QUERY_ERROR", queryErrorMessage);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: $"Video request query fail, query count: {(videoRequest.QueryCount + 1).ToString()}",
                        reference: new
                        {
                            videoRequest.ScopeKey,
                            ClientDomain = videoRequest.DomainName,
                            videoRequest.RefContentType,
                            videoRequest.RefContentId,
                            RefVideoRequestId = videoRequest.Id
                        },
                        facility: VideoRequestOperationFacilities.VIDEO_REQUEST_QUERY_GENERATION_FAIL,
                        correlationId: videoRequest.CorrelationId,
                        exception: null
                    ));
                }
                else
                {
                    _logger.LogWarning("VideoRequest[{VideoRequestId}] | {QueryResult} | {ErrorMessage}", videoRequest.Id.ToString(), "QUERY_ERROR", queryErrorMessage);
                }
            }
        }
    }

    public async Task VideoFileDownloadToLocalAsync(Guid videoRequestId)
    {
        if (videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool videoDownloadOperationSuccess;
        string errorMessage = null;
        string localVideoPath = null;
        try
        {
            if (string.IsNullOrWhiteSpace(videoRequest.ExternalVideoUrl))
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] external video url can not be empty", videoRequest.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequest.ExternalVideoUrl));
            }

            var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == videoRequest.ScopeKey);
            if (customerVpSetting is null) throw new ArgumentNullException(nameof(videoRequest.ScopeKey));

            var videoDownloadRequest = new VideoGenerationDownloadRequestDto { ExternalVideoUrl = videoRequest.ExternalVideoUrl };

            var response = customerVpSetting.VideoGenerationProviderType switch
            {
                VideoGenerationProviderTypes.COLOSSYAN_AI => await colossyanAiVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.YEPIC_AI => await yepicAiVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.DID_AI => await didAiVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.HEYGEN_AI => await heyGenVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.CREATOMATE => await creatomateVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                _ => throw new Exception("Unknown video generation provider type")
            };

            if (response is { HasError: false })
            {
                localVideoPath = response.LocalVideoPath;
                videoDownloadOperationSuccess = !string.IsNullOrWhiteSpace(localVideoPath);
            }
            else throw new Exception(response?.ErrorMessage ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_DOWNLOAD_FAIL,
                correlationId: videoRequest.CorrelationId,
                exception: ex
            ));
            videoDownloadOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedVideoRequest = await videoRequestRepository.SetVideoFileDownloadResultAsync(
            id: videoRequestId,
            isDownloadSuccess: videoDownloadOperationSuccess,
            errorMessage: errorMessage,
            localVideoPath: localVideoPath);

        if (updatedVideoRequest.OperationStatus == VideoRequestStates.VideoDownloadedWaitForVideoStorageUpload)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Video file download success",
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_DOWNLOAD_SUCCESS,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoFileUploadToStorageStartedEto(
                    VideoRequestId: updatedVideoRequest.Id
                ));
        }
    }

    public async Task VideoFileUploadToStorageAsync(Guid videoRequestId)
    {
        if (videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool videoUploadOperationSuccess;
        string errorMessage = null;
        string storageVideoTraceId = null;
        string storageVideoUrl = null;
        try
        {
            if (string.IsNullOrWhiteSpace(videoRequest.ExternalVideoUrl))
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] external video url can not be empty", videoRequest.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequest.ExternalVideoUrl));
            }

            var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == videoRequest.ScopeKey);
            if (customerVpSetting is null) throw new ArgumentNullException(nameof(videoRequest.ScopeKey));

            var videoUploadRequest = new FileStorageUploadRequestDto { LocalFilePath = videoRequest.LocalVideoPath };

            var response = customerVpSetting.VideoGenerationProviderType switch
            {
                VideoGenerationProviderTypes.COLOSSYAN_AI => await bunnyCdnSelfVideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnSelfStorageSettings,
                    customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.YEPIC_AI => await bunnyCdnSelfVideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnSelfStorageSettings,
                    customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.DID_AI => await bunnyCdnS3VideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnS3StorageSettings,
                    customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.HEYGEN_AI => await bunnyCdnS3VideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnS3StorageSettings,
                    customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.CREATOMATE => await bunnyCdnS3VideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnS3StorageSettings,
                    customerVpSetting.IsCustomerZoneActive ? customerVpSetting.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                _ => throw new Exception("Unknown video generation provider type")
            };

            if (response is { HasError: false })
            {
                storageVideoTraceId = response.FileTraceId;
                storageVideoUrl = response.FileStorageUrl;
                videoUploadOperationSuccess = !string.IsNullOrWhiteSpace(storageVideoUrl);
            }
            else throw new Exception(response?.ErrorMessage ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_FAIL,
                correlationId: videoRequest.CorrelationId,
                exception: ex
            ));
            videoUploadOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedVideoRequest = await videoRequestRepository.SetVideoFileStorageUploadResultAsync(
            id: videoRequestId,
            isUploadSuccess: videoUploadOperationSuccess,
            errorMessage: errorMessage,
            storageVideoTraceId: !string.IsNullOrWhiteSpace(storageVideoTraceId) ? storageVideoTraceId : null,
            storageVideoUrl: videoUploadOperationSuccess ? storageVideoUrl : null);

        if (videoUploadOperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Video file storage upload success",
                reference: new
                {
                    videoRequest.ScopeKey,
                    ClientDomain = videoRequest.DomainName,
                    videoRequest.RefContentType,
                    videoRequest.RefContentId,
                    RefVideoRequestId = videoRequest.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_SUCCESS,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            try
            {
                if (!string.IsNullOrWhiteSpace(videoRequest.LocalVideoPath))
                    File.Delete(videoRequest.LocalVideoPath);
            }
            catch (IOException)
            {
                _logger.LogError("File streamer still in use");
            }
            catch (Exception ex)
            {
                _logger.LogError("File could not be deleted - Reason: {1}", ex.Message);
            }
        }

        //  Integration Event for ContentService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoGenerationResultEto(
                ReferenceContentType: (ReferenceContentTypes)updatedVideoRequest.RefContentType,
                ReferenceContentId: updatedVideoRequest.RefContentId,
                IsGenerateSuccess: updatedVideoRequest.OperationStatus == VideoRequestStates.OperationSuccess,
                VideoRequestId: updatedVideoRequest.Id,
                StorageVideoUrl: updatedVideoRequest.StorageVideoUrl
            ));
    }

    public async Task SetStatusToFailedAsync(Guid videoRequestId, string failedReason, string correlationId = null)
    {
        if (videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placedVideoRequest = await videoRequestRepository.SetStatusToFailedAsync(id: videoRequestId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Video request status fail: {failedReason ?? string.Empty}",
            reference: new
            {
                placedVideoRequest.ScopeKey,
                ClientDomain = placedVideoRequest.DomainName,
                placedVideoRequest.RefContentType,
                placedVideoRequest.RefContentId,
                RefVideoRequestId = placedVideoRequest.Id
            },
            facility: VideoRequestOperationFacilities.VIDEO_REQUEST_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }
}