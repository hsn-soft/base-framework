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
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.Enums;
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

public sealed class VideoOperationsAppService : ApplicationServiceBase, IVideoOperationsAppService
{
    private readonly IFrameworkLogger _logger;

    private readonly IColossyanAiVideoGenerationProvider _colossyanAiVideoGenerationProvider;
    private readonly ColossyanAiSettings _colossyanAiSettings;

    private readonly IYepicAiVideoGenerationProvider _yepicAiVideoGenerationProvider;
    private readonly YepicAiSettings _yepicAiSettings;

    private readonly IDidAiVideoGenerationProvider _didAiVideoGenerationProvider;
    private readonly DidAiSettings _didAiSettings;

    private readonly IHeyGenVideoGenerationProvider _heyGenVideoGenerationProvider;
    private readonly HeyGenSettings _heyGenSettings;

    private readonly ICreatomateVideoGenerationProvider _creatomateVideoGenerationProvider;
    private readonly CreatomateSettings _creatomateSettings;

    private readonly IBunnyCdnSelfVideoStorageProvider _bunnyCdnSelfVideoStorageProvider;
    private readonly BunnyCdnSelfStorageSettings _bunnyCdnSelfStorageSettings;

    private readonly IBunnyCdnS3VideoStorageProvider _bunnyCdnS3VideoStorageProvider;
    private readonly BunnyCdnS3StorageSettings _bunnyCdnS3StorageSettings;

    private readonly IElevenLabsAudioProvider _elevenLabsAudioProvider;
    private readonly ElevenLabsSettings _elevenLabsSettings;

    private readonly VideoGenerationSettings _serviceSettings;
    private readonly VideoRequestQuerySettings _querySettings;

    private readonly ICustomerConfigurationRepository _customerSettingsRepository;
    private readonly IVideoRequestRepository _videoRequestRepository;

    public VideoOperationsAppService(IServiceProvider provider,
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
        ICustomerConfigurationRepository customerConfigurationRepository,
        IVideoRequestRepository videoRequestRepository)
        : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();

        _colossyanAiVideoGenerationProvider = colossyanAiVideoGenerationProvider;
        _colossyanAiSettings = colossyanAiSettings?.Value ?? throw new ArgumentNullException(nameof(colossyanAiSettings));

        _yepicAiVideoGenerationProvider = yepicAiVideoGenerationProvider;
        _yepicAiSettings = yepicAiSettings?.Value ?? throw new ArgumentNullException(nameof(yepicAiSettings));

        _didAiVideoGenerationProvider = didAiVideoGenerationProvider;
        _didAiSettings = didAiSettings?.Value ?? throw new ArgumentNullException(nameof(didAiSettings));

        _heyGenVideoGenerationProvider = heyGenVideoGenerationProvider;
        _heyGenSettings = heyGenSettings?.Value ?? throw new ArgumentNullException(nameof(heyGenSettings));

        _creatomateVideoGenerationProvider = creatomateVideoGenerationProvider;
        _creatomateSettings = creatomateSettings?.Value ?? throw new ArgumentNullException(nameof(creatomateSettings));

        _bunnyCdnSelfVideoStorageProvider = bunnyCdnSelfVideoStorageProvider;
        _bunnyCdnSelfStorageSettings = bunnyCdnSelfStorageSettings?.Value ?? throw new ArgumentNullException(nameof(bunnyCdnSelfStorageSettings));

        _bunnyCdnS3VideoStorageProvider = bunnyCdnS3VideoStorageProvider;
        _bunnyCdnS3StorageSettings = bunnyCdnS3StorageSettings?.Value ?? throw new ArgumentNullException(nameof(bunnyCdnS3StorageSettings));

        _elevenLabsAudioProvider = elevenLabsAudioProvider;
        _elevenLabsSettings = elevenLabsSettings?.Value ?? throw new ArgumentNullException(nameof(elevenLabsSettings));

        _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));
        _querySettings = querySettings?.Value ?? throw new ArgumentNullException(nameof(querySettings));

        _customerSettingsRepository = customerConfigurationRepository;
        _videoRequestRepository = videoRequestRepository;
    }

    public async Task VideoRequestCreateAsync(VideoGenerationStartedEto input, string correlationId = null)
    {
        if (input == null || input.TenantId == Guid.Empty || input.ClientId == Guid.Empty || input.ReferenceContentId == Guid.Empty || input.EncodedNormalizedContentDatas is { Count: < 1 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await _videoRequestRepository.FindByUniqueKeysAsync(input.ClientId, input.ReferenceContentId);
        if (videoRequest != null) return;

        var placed = await _videoRequestRepository.CreateAsync(
            tenantId: input.TenantId,
            clientId: input.ClientId,
            domainName: input.DomainName,
            refContentType: (ReferenceContentTypes)input.ReferenceContentType,
            refContentId: input.ReferenceContentId,
            operationStatus: VideoRequestStates.CreatedWaitForVideoSent,
            normalizedContentDatas: Mapper.Map<List<EncodedNormalizedContentData>, List<NormalizedContentData>>(input.EncodedNormalizedContentDatas),
            correlationId: correlationId);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"Video request created",
            reference: new
            {
                placed.TenantId,
                placed.ClientId,
                ClientDomain = placed.DomainName,
                placed.RefContentType,
                placed.RefContentId,
                VideoRequestId = placed.Id
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
                VideoRequestId: placed.Id
            ));

        var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(placed.ClientId);
        if (clientSettings is null) throw new ArgumentNullException(nameof(placed.ClientId));

        bool isProviderAudioOperationEnabled = clientSettings.VideoGenerationProviderType switch
        {
            VideoGenerationProviderTypes.COLOSSYAN_AI => _colossyanAiSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.YEPIC_AI => _yepicAiSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.DID_AI => _didAiSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.HEYGEN_AI => _heyGenSettings.IsProviderAudioOperationEnabled,
            VideoGenerationProviderTypes.CREATOMATE => _creatomateSettings.IsProviderAudioOperationEnabled,
            _ => throw new Exception("Unknown audio generation provider type")
        };

        if (isProviderAudioOperationEnabled && clientSettings.IsEnabledExternalAudioGeneration)
        {
            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioSendStartedEto(
                    VideoRequestId: placed.Id
                ));
        }
        else
        {
            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoRequestSendStartedEto(
                    VideoRequestId: placed.Id
                ));
        }
    }

    public async Task AudioSendAsync(Guid videoRequestId)
    {
        if (videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequestItem = await _videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        var audioRequestSendOperationSuccess = false;
        var audioFileNames = new List<string>();
        string errorMessage = null;

        var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(videoRequestItem.ClientId);
        if (clientSettings is null) throw new ArgumentNullException(nameof(videoRequestItem.ClientId));

        try
        {
            if (videoRequestItem.NormalizedContentDatas is { Count: < 1 })
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] content data can not be empty",
                    videoRequestItem.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequestItem.NormalizedContentDatas));
            }

            int index = 0;
            foreach (var normalizedContentData in videoRequestItem.NormalizedContentDatas)
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
                        var audioGenerationSendRequest = new AudioGenerationSendRequestDto { AudioContent = normalizedContentData.NormalizedContent, VideoRequestReferenceId = videoRequestItem.Id + "-" + index };

                        response = await _elevenLabsAudioProvider.SendAudioAsync(audioGenerationSendRequest,
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
                                videoRequestItem.TenantId,
                                videoRequestItem.ClientId,
                                ClientDomain = videoRequestItem.DomainName,
                                videoRequestItem.RefContentType,
                                videoRequestItem.RefContentId,
                                VideoRequestId = videoRequestItem.Id
                            },
                            facility: VideoRequestOperationFacilities.AUDIO_REQUEST_SEND_SUCCESS,
                            correlationId: videoRequestItem.CorrelationId,
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
                            videoRequestItem.TenantId,
                            videoRequestItem.ClientId,
                            ClientDomain = videoRequestItem.DomainName,
                            videoRequestItem.RefContentType,
                            videoRequestItem.RefContentId,
                            VideoRequestId = videoRequestItem.Id
                        },
                        facility: VideoRequestOperationFacilities.AUDIO_REQUEST_SEND_FAIL,
                        correlationId: videoRequestItem.CorrelationId,
                        exception: ex
                    ));

                    audioRequestSendOperationSuccess = false;
                    errorMessage = ex.Message;
                }

                if (errorMessage != null)
                    break;
            }

            await _videoRequestRepository.SetAudioRequestSendResultAsync(
                id: videoRequestId,
                isSendSuccess: audioRequestSendOperationSuccess,
                errorMessage: errorMessage,
                audioFileNames: audioRequestSendOperationSuccess ? audioFileNames : null);

            if (audioRequestSendOperationSuccess)
            {
                if (!_serviceSettings.SkipAudioGenerationOperation)
                {
                    bool isEnabledWaitAudioFileGeneration = clientSettings.VideoGenerationProviderType switch
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
                        bool isEnabledAudioFileDownloadOperation = clientSettings.VideoGenerationProviderType switch
                        {
                            VideoGenerationProviderTypes.COLOSSYAN_AI => _colossyanAiSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.YEPIC_AI => _yepicAiSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.DID_AI => _didAiSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.HEYGEN_AI => _heyGenSettings
                                .IsEnabledAudioFileDownloadOperation,
                            VideoGenerationProviderTypes.CREATOMATE => _creatomateSettings.
                                IsEnabledAudioFileDownloadOperation,
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
                        await _videoRequestRepository.SetAudioGenerationResultAsync(id: videoRequestId);
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.AUDIO_REQUEST_SEND_FAIL,
                correlationId: videoRequestItem.CorrelationId,
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

        var videoRequestItem = await _videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool audioUploadOperationSuccess = false;
        string errorMessage = null;
        List<string> audioTraceIds = new List<string>();
        List<string> audioUrls = new List<string>();

        var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(videoRequestItem.ClientId);
        try
        {
            if (videoRequestItem.AudioFileNames is { Count: < 1 })
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] AudioFileNames can not be empty", videoRequestItem.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequestItem.AudioFileNames));
            }

            foreach (var audioFileName in videoRequestItem.AudioFileNames)
            {
                var audioUploadRequest = new FileStorageUploadRequestDto { LocalFilePath = audioFileName };

                var response = clientSettings.VideoGenerationProviderType switch
                {
                    VideoGenerationProviderTypes.COLOSSYAN_AI => await _bunnyCdnSelfVideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnSelfStorageSettings,
                        clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.YEPIC_AI => await _bunnyCdnSelfVideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnSelfStorageSettings,
                        clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.DID_AI => await _bunnyCdnS3VideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnS3StorageSettings,
                        clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.HEYGEN_AI => await _bunnyCdnS3VideoStorageProvider.UploadAsync(audioUploadRequest, _bunnyCdnS3StorageSettings,
                        clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                    VideoGenerationProviderTypes.CREATOMATE => await _bunnyCdnS3VideoStorageProvider.UploadAsync(audioUploadRequest,_bunnyCdnS3StorageSettings,
                        clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.AUDIO_FILE_STORAGE_UPLOAD_FAIL,
                correlationId: videoRequestItem.CorrelationId,
                exception: ex
            ));
            audioUploadOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedVideoRequest = await _videoRequestRepository.SetAudioFileStorageUploadResultAsync(
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.AUDIO_FILE_STORAGE_UPLOAD_SUCCESS,
                correlationId: videoRequestItem.CorrelationId,
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
                ReferenceContentType: (ReferenceContentTypes)updatedVideoRequest.RefContentType,
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

        var videoRequestItem = await _videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool videoRequestSendOperationSuccess;
        string errorMessage = null;
        string externalVideoTraceId = null;
        try
        {
            if (videoRequestItem.NormalizedContentDatas is { Count: < 1 })
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] content data can not be empty", videoRequestItem.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequestItem.NormalizedContentDatas));
            }

            var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(videoRequestItem.ClientId);
            if (clientSettings is null) throw new ArgumentNullException(nameof(videoRequestItem.ClientId));

            VideoGenerationSendResponseDto response;

            if (!_serviceSettings.SkipVideoGenerationOperation)
            {
                if (clientSettings.IsEnabledVideoGeneration)
                {
                    if (videoRequestItem.RefContentType != ReferenceContentTypes.APP_REQUEST_CONTENT && videoRequestItem.RefContentType != ReferenceContentTypes.ANALYSIS_CONTENT)
                    {
                        _logger.LogError("VideoRequest[{VideoRequestReferenceId}] RefContentType is invalid", videoRequestItem.Id.ToString());
                        throw new Exception($"VideoRequest[{videoRequestItem.Id.ToString()}] RefContentType is invalid");
                    }

                    var videoGenerationSendRequest = new VideoGenerationSendRequestDto
                    {
                        VideoRequestId = videoRequestItem.Id,
                        RefContentType = videoRequestItem.RefContentType,
                        VideoContentDatas = Mapper.Map<List<NormalizedContentData>, List<VideoContentDataDto>>(videoRequestItem.NormalizedContentDatas),
                        AudioFileNames = videoRequestItem.AudioFileNames
                    };
                    response = clientSettings.VideoGenerationProviderType switch
                    {
                        VideoGenerationProviderTypes.COLOSSYAN_AI => await _colossyanAiVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _colossyanAiSettings.IsProviderSupportPreSignedStorage,
                            (ClientColossyanAiSettings)clientSettings.VideoGenerationProviderSettings, _bunnyCdnSelfStorageSettings,
                            clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.YEPIC_AI => await _yepicAiVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _yepicAiSettings.IsProviderSupportPreSignedStorage,
                            (ClientYepicAiSettings)clientSettings.VideoGenerationProviderSettings, _bunnyCdnSelfStorageSettings,
                            clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.DID_AI => await _didAiVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _didAiSettings.IsProviderSupportPreSignedStorage,
                            (ClientDidAiSettings)clientSettings.VideoGenerationProviderSettings, _bunnyCdnS3StorageSettings,
                            clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.HEYGEN_AI => await _heyGenVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _heyGenSettings.IsProviderSupportPreSignedStorage,
                            (ClientHeyGenSettings)clientSettings.VideoGenerationProviderSettings, _bunnyCdnS3StorageSettings,
                            clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                        VideoGenerationProviderTypes.CREATOMATE => await _creatomateVideoGenerationProvider.SendVideoRequestAsync(videoGenerationSendRequest, _creatomateSettings.IsProviderSupportPreSignedStorage,
                            (ClientCreatomateSettings)clientSettings.VideoGenerationProviderSettings, _bunnyCdnS3StorageSettings,
                            clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_REQUEST_SEND_FAIL,
                correlationId: videoRequestItem.CorrelationId,
                exception: ex
            ));
            videoRequestSendOperationSuccess = false;
            errorMessage = ex.Message;
        }

        await _videoRequestRepository.SetVideoRequestSendResultAsync(
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_REQUEST_SEND_SUCCESS,
                correlationId: videoRequestItem.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoRequestQueryEto(
                    VideoRequestId: videoRequestItem.Id
                ));
        }
    }

    public async Task VideoRequestQueryAsync(VideoRequestQueryEto input, string correlationId = null)
    {
        if (input?.VideoRequestId == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var videoRequest = await _videoRequestRepository.GetByIdOrDefaultAsync(input.VideoRequestId);
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
                var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(videoRequest.ClientId) ?? throw new ArgumentNullException(nameof(videoRequest.ClientId));
                if (!clientSettings.IsEnabledVideoGeneration)
                {
                    // block re-query operation
                    videoRequest.QueryCount = _querySettings.ReQueryLimit;
                    throw new Exception("VIDEO_GENERATION_DISABLED");
                }

                var videoGenerationQueryRequest = new VideoGenerationQueryRequestDto { ExternalVideoTraceId = videoRequest.ExternalVideoTraceId };
                switch (clientSettings.VideoGenerationProviderType)
                {
                    case VideoGenerationProviderTypes.COLOSSYAN_AI:
                        {
                            response = await _colossyanAiVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _colossyanAiSettings.IsProviderSupportPreSignedStorage,
                                (ClientColossyanAiSettings)clientSettings.VideoGenerationProviderSettings,
                                _bunnyCdnSelfStorageSettings,
                                clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.YEPIC_AI:
                        {
                            response = await _yepicAiVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _yepicAiSettings.IsProviderSupportPreSignedStorage,
                                (ClientYepicAiSettings)clientSettings.VideoGenerationProviderSettings,
                                _bunnyCdnSelfStorageSettings,
                                clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.DID_AI:
                        {
                            response = await _didAiVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _didAiSettings.IsProviderSupportPreSignedStorage,
                                (ClientDidAiSettings)clientSettings.VideoGenerationProviderSettings,
                                _bunnyCdnS3StorageSettings,
                                clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.HEYGEN_AI:
                        {
                            response = await _heyGenVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _heyGenSettings.IsProviderSupportPreSignedStorage,
                                (ClientHeyGenSettings)clientSettings.VideoGenerationProviderSettings,
                                _bunnyCdnS3StorageSettings,
                                clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName);
                            break;
                        }
                    case VideoGenerationProviderTypes.CREATOMATE:
                        {
                            response = await _creatomateVideoGenerationProvider.QueryVideoRequestAsync(videoGenerationQueryRequest,
                                _creatomateSettings.IsProviderSupportPreSignedStorage,
                                (ClientCreatomateSettings)clientSettings.VideoGenerationProviderSettings,
                                _bunnyCdnS3StorageSettings,
                                clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName);
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
            var updatedVideoRequest = await _videoRequestRepository.SetVideoRequestQueryResultAsync(
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
                        videoRequest.TenantId,
                        videoRequest.ClientId,
                        ClientDomain = videoRequest.DomainName,
                        videoRequest.RefContentType,
                        videoRequest.RefContentId,
                        VideoRequestId = videoRequest.Id
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
                        updatedVideoRequest.TenantId,
                        updatedVideoRequest.ClientId,
                        ClientDomain = videoRequest.DomainName,
                        updatedVideoRequest.RefContentType,
                        updatedVideoRequest.RefContentId,
                        VideoRequestId = updatedVideoRequest.Id
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
            await _videoRequestRepository.SetVideoRequestQueryResultAsync(
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
                        videoRequest.TenantId,
                        videoRequest.ClientId,
                        ClientDomain = videoRequest.DomainName,
                        videoRequest.RefContentType,
                        videoRequest.RefContentId,
                        VideoRequestId = videoRequest.Id
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
                            videoRequest.TenantId,
                            videoRequest.ClientId,
                            ClientDomain = videoRequest.DomainName,
                            videoRequest.RefContentType,
                            videoRequest.RefContentId,
                            VideoRequestId = videoRequest.Id
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

        var videoRequestItem = await _videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool videoDownloadOperationSuccess;
        string errorMessage = null;
        string localVideoPath = null;
        try
        {
            if (string.IsNullOrWhiteSpace(videoRequestItem.ExternalVideoUrl))
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] external video url can not be empty", videoRequestItem.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequestItem.ExternalVideoUrl));
            }

            var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(videoRequestItem.ClientId);

            var videoDownloadRequest = new VideoGenerationDownloadRequestDto { ExternalVideoUrl = videoRequestItem.ExternalVideoUrl };

            var response = clientSettings.VideoGenerationProviderType switch
            {
                VideoGenerationProviderTypes.COLOSSYAN_AI => await _colossyanAiVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.YEPIC_AI => await _yepicAiVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.DID_AI => await _didAiVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.HEYGEN_AI => await _heyGenVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
                VideoGenerationProviderTypes.CREATOMATE => await _creatomateVideoGenerationProvider.DownloadAsync(videoDownloadRequest),
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_DOWNLOAD_FAIL,
                correlationId: videoRequestItem.CorrelationId,
                exception: ex
            ));
            videoDownloadOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedVideoRequest = await _videoRequestRepository.SetVideoFileDownloadResultAsync(
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_DOWNLOAD_SUCCESS,
                correlationId: videoRequestItem.CorrelationId,
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

        var videoRequestItem = await _videoRequestRepository.GetByIdOrDefaultAsync(videoRequestId);
        if (videoRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool videoUploadOperationSuccess;
        string errorMessage = null;
        string storageVideoTraceId = null;
        string storageVideoUrl = null;
        try
        {
            if (string.IsNullOrWhiteSpace(videoRequestItem.ExternalVideoUrl))
            {
                _logger.LogError("VideoRequest[{VideoRequestReferenceId}] external video url can not be empty", videoRequestItem.Id.ToString());
                throw new ArgumentNullException(nameof(videoRequestItem.ExternalVideoUrl));
            }

            var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(videoRequestItem.ClientId);

            var videoUploadRequest = new FileStorageUploadRequestDto { LocalFilePath = videoRequestItem.LocalVideoPath };

            var response = clientSettings.VideoGenerationProviderType switch
            {
                VideoGenerationProviderTypes.COLOSSYAN_AI => await _bunnyCdnSelfVideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnSelfStorageSettings,
                    clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.YEPIC_AI => await _bunnyCdnSelfVideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnSelfStorageSettings,
                    clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnSelfStorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.DID_AI => await _bunnyCdnS3VideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnS3StorageSettings,
                    clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.HEYGEN_AI => await _bunnyCdnS3VideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnS3StorageSettings,
                    clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
                VideoGenerationProviderTypes.CREATOMATE => await _bunnyCdnS3VideoStorageProvider.UploadAsync(videoUploadRequest, _bunnyCdnS3StorageSettings,
                    clientSettings.IsCustomerZoneActive ? clientSettings.CustomerZoneName : _bunnyCdnS3StorageSettings.DefaultZoneName),
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_FAIL,
                correlationId: videoRequestItem.CorrelationId,
                exception: ex
            ));
            videoUploadOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedVideoRequest = await _videoRequestRepository.SetVideoFileStorageUploadResultAsync(
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
                    videoRequestItem.TenantId,
                    videoRequestItem.ClientId,
                    ClientDomain = videoRequestItem.DomainName,
                    videoRequestItem.RefContentType,
                    videoRequestItem.RefContentId,
                    VideoRequestId = videoRequestItem.Id
                },
                facility: VideoRequestOperationFacilities.VIDEO_FILE_STORAGE_UPLOAD_SUCCESS,
                correlationId: videoRequestItem.CorrelationId,
                exception: null
            ));

            try
            {
                if (!string.IsNullOrWhiteSpace(videoRequestItem.LocalVideoPath))
                    File.Delete(videoRequestItem.LocalVideoPath);
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

        var placed = await _videoRequestRepository.SetStatusToFailedAsync(id: videoRequestId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Video request status fail: {failedReason ?? string.Empty}",
            reference: new
            {
                placed.TenantId,
                placed.ClientId,
                ClientDomain = placed.DomainName,
                placed.RefContentType,
                placed.RefContentId
            },
            facility: VideoRequestOperationFacilities.VIDEO_REQUEST_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }
}