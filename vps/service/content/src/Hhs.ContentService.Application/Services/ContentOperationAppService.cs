using Hhs.ContentService.Application.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Exceptions;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.ContentService.Domain.Settings;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hhs.ContentService.Application.Services;

public sealed class ContentOperationService(
    IServiceProvider provider,
    IOptions<ContentOperationSettings> serviceSettings,
    ICustomerContentRepository customerContentRepository,
    IAnalysisContentRepository analysisContentRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IContentVideoGenerationLimitRepository contentVideoGenerationLimitRepository) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();
    private readonly ContentOperationSettings _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));

    public async Task HandleNormalizedRequestReferenceAsync(ContentType refContentType, Guid refContentId, Guid refNormalizeRequestId, string normalizeStatus, string normalizeCurrentStep, [CanBeNull] string correlationId = null)
    {
        switch (refContentType)
        {
            case ContentType.CustomerContent:
                await customerContentRepository.SetNormalizedReferenceAsync(refContentId, refNormalizeRequestId, normalizeStatus, normalizeCurrentStep);
                break;
            case ContentType.AnalysisContent:
                await analysisContentRepository.SetNormalizedReferenceAsync(refContentId, refNormalizeRequestId, normalizeStatus, normalizeCurrentStep);
                break;
            case ContentType.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(refContentType), refContentType, null);
        }

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.CustomerContentNormalizeRequestCreated,
            reference: new { Type = refContentType.ToString(), Key = refContentId, RefType = "CustomerContentNormalizeRequest", RefKey = refNormalizeRequestId },
            facility: Facilities.NormalizeRequestReferenceSet,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task HandleCustomerContentScrapeResultsAsync(CustomerContentScrapingCompletedEto @event, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default)
    {
        await customerContentRepository.SetScrapeResultsAsync(
            @event.CustomerContentId,
            @event.CustomerContentNormalizeRequestId,
            @event.NormalizeStatus,
            @event.NormalizeCurrentStep,
            @event.ScrapedReleaseTimeUtc);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.CustomerContentScrapingCompleted,
            reference: new { Type = nameof(CustomerContent), Key = @event.CustomerContentId, RefType = "CustomerContentNormalizeRequest", RefKey = @event.CustomerContentNormalizeRequestId },
            facility: Facilities.CustomerContentScrapingCompleted,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task HandleOutlineResultAsync(NormalizerResultPublishedEto @event, CancellationToken cancellationToken = default)
    {
        bool shouldPublishEvent = false;
        string? scopeKey = null;
        string? correlationId = null;

        switch (@event.RefContentType)
        {
            case ContentType.CustomerContent:
                {
                    var entity = await customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

                    if (entity != null && entity.NormalizeStatus != NormalizeStatusNames.Completed)
                    {
                        if (@event.NormalizeStatus == NormalizeStatusNames.OutlineSkipped)
                        {
                            await customerContentRepository.SetNormalizedResultsAsync(
                                entity.Id,
                                @event.NormalizeStatus,
                                @event.NormalizeCurrentStep);

                            _logger.FrameworkInfoLog(LogHelper.Generate(
                                message: "CustomerContent video generation rejected",
                                reference: new
                                {
                                    entity.ScopeKey,
                                    Type = nameof(CustomerContent),
                                    Key = entity.Id,
                                    RefType = "CustomerContentNormalizeRequest",
                                    RefKey = entity.NormalizeRequestId
                                },
                                facility: Facilities.ContentOutlineRejected,
                                correlationId: entity.CorrelationId,
                                exception: null
                            ));

                            return;
                        }

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: "CustomerContent normalized success",
                            reference: new
                            {
                                entity.ScopeKey,
                                Type = nameof(CustomerContent),
                                Key = entity.Id,
                                RefType = "CustomerContentNormalizeRequest",
                                RefKey = entity.NormalizeRequestId
                            },
                            facility: Facilities.ContentNormalizedSuccess,
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));

                        var checkResult = await CheckVideoGenerationApproveRules(entity.ScopeKey, entity.ScrapReleaseTimeUtc);
                        if (checkResult.Key)
                        {
                            await customerContentRepository.SetVideoGenerationApprovedAsync(id: entity.Id);

                            _logger.FrameworkInfoLog(LogHelper.Generate(
                                message: "CustomerContent video generation approved",
                                reference: new
                                {
                                    entity.ScopeKey,
                                    Type = nameof(CustomerContent),
                                    Key = entity.Id,
                                    RefType = "CustomerContentNormalizeRequest",
                                    RefKey = entity.NormalizeRequestId
                                },
                                facility: Facilities.VideoGenerationApproved,
                                correlationId: entity.CorrelationId,
                                exception: null
                            ));

                            // Add video generation history for client quote control
                            await contentVideoGenerationLimitRepository.CreateAsync(scopeKey: entity.ScopeKey,
                                videoGenerationDate: entity.ScrapReleaseTimeUtc?.Date ?? DateTime.UtcNow.Date,
                                videoGenerationType: VideoGenerationTypes.DirectVideoGeneration,
                                contentReferenceIds: entity.Id.ToString());

                            scopeKey = entity.ScopeKey;
                            correlationId = entity.CorrelationId;
                            shouldPublishEvent = true;
                        }
                        else
                        {
                            await customerContentRepository.SetVideoGenerationRejectedAsync(entity.Id, checkResult.Value);

                            _logger.FrameworkInfoLog(LogHelper.Generate(
                                message: "CustomerContent video generation rejected",
                                reference: new
                                {
                                    entity.ScopeKey,
                                    Type = nameof(CustomerContent),
                                    Key = entity.Id,
                                    RefType = "CustomerContentNormalizeRequest",
                                    RefKey = entity.NormalizeRequestId,
                                    RejectReason = checkResult.Value
                                },
                                facility: Facilities.VideoGenerationRejected,
                                correlationId: entity.CorrelationId,
                                exception: null
                            ));
                        }
                    }

                    break;
                }
            case ContentType.AnalysisContent:
                {
                    var entity = await analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);

                    if (entity != null && entity.NormalizeStatus != NormalizeStatusNames.Completed)
                    {
                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: "Analysis Content normalized success",
                            reference: new
                            {
                                entity.ScopeKey,
                                Type = nameof(AnalysisContent),
                                Key = entity.Id,
                                RefType = "AnalysisContentNormalizeRequest",
                                RefKey = entity.NormalizeRequestId
                            },
                            facility: Facilities.ContentNormalizedSuccess,
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));

                        await analysisContentRepository.SetVideoGenerationApprovedAsync(id: entity.Id);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: "Analysis Content video generation approved",
                            reference: new
                            {
                                entity.ScopeKey,
                                Type = nameof(AnalysisContent),
                                Key = entity.Id,
                                RefType = "AnalysisContentNormalizeRequest",
                                RefKey = entity.NormalizeRequestId
                            },
                            facility: Facilities.VideoGenerationApproved,
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));

                        // Add video generation history for client quote control
                        await contentVideoGenerationLimitRepository.CreateAsync(scopeKey: entity.ScopeKey,
                            videoGenerationDate: entity.AnalysisDate.Date,
                            videoGenerationType: VideoGenerationTypes.AnalysisVideoGeneration,
                            contentReferenceIds: entity.Id.ToString());

                        scopeKey = entity.ScopeKey;
                        correlationId = entity.CorrelationId;
                        shouldPublishEvent = true;
                    }

                    break;
                }
            case ContentType.None:
            default:
                throw new InvalidOperationException("Unknown content reference type for approve operation");
        }

        if (shouldPublishEvent)
        {
            await EventBus.PublishAsync(
                parentMessage: ParentIntegrationEvent,
                correlationId: correlationId,
                eventMessage: new VideoGenerationApprovedEto
                {
                    RefContentId = @event.RefContentId,
                    RefContentType = @event.RefContentType,
                    ScopeKey = string.IsNullOrWhiteSpace(scopeKey)
                        ? throw new ArgumentNullException(scopeKey)
                        : scopeKey,
                    RefNormalizeRequestId = @event.NormalizeRequestId
                }
            );
        }
    }

    public async Task HandleVideoRequestCreatedAsync(Guid refContentId, Guid refVideoRequestId, ContentType refContentType, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default)
    {
        switch (refContentType)
        {
            case ContentType.CustomerContent:
                await customerContentRepository.SetVideoReferenceAsync(refContentId, refVideoRequestId);
                break;
            case ContentType.AnalysisContent:
                await analysisContentRepository.SetVideoReferenceAsync(refContentId, refVideoRequestId);
                break;
            case ContentType.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(refContentType), refContentType, null);
        }

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.VideoRequestCreated,
            reference: new { Type = refContentType.ToString(), Key = refContentId, RefType = "VideoRequest", RefKey = refVideoRequestId },
            facility: Facilities.VideoRequestCreated,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task HandleAudioOperationStartedAsync(AudioOperationStartedEto @event, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default)
    {
        switch (@event.RefContentType)
        {
            case ContentType.CustomerContent:
                await customerContentRepository.SetAudioOperationStartedAsync(@event.RefContentId, @event.AudioMode);
                break;
            case ContentType.AnalysisContent:
                await analysisContentRepository.SetAudioOperationStartedAsync(@event.RefContentId, @event.AudioMode);
                break;
            case ContentType.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(@event.RefContentType), @event.RefContentType, null);
        }

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: @event.AudioMode,
            reference: new
            {
                Type = @event.RefContentType.ToString(),
                Key = @event.RefContentId,
                RefType = "VideoRequest",
                RefKey = @event.VideoRequestId,
                AudioMode = @event.AudioMode
            },
            facility: Facilities.AudioOperationStarted,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task HandleVideoProviderStartedAsync(VideoProviderRequestStartedEto @event, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default)
    {
        switch (@event.RefContentType)
        {
            case ContentType.CustomerContent:
                await customerContentRepository.SetVideoProviderStartedAsync(@event.RefContentId);
                break;
            case ContentType.AnalysisContent:
                await analysisContentRepository.SetVideoProviderStartedAsync(@event.RefContentId);
                break;
            case ContentType.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(@event.RefContentType), @event.RefContentType, null);
        }

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.VideoProviderRequestStarted,
            reference: new { Type = @event.RefContentType.ToString(), Key = @event.RefContentId, RefType = "VideoRequest", RefKey = @event.VideoRequestId },
            facility: Facilities.VideoProviderRequestStarted,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task HandleVideoResultAsync(VideoGenerationResultPublishedEto @event, CancellationToken cancellationToken = default)
    {
        switch (@event.RefContentType)
        {
            case ContentType.CustomerContent:
                {
                    var entity = await customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);
                    if (entity != null)
                    {
                        entity.VideoStatus = MediaStatusNames.Completed;
                        entity.VideoRequestId = @event.VideoRequestId;
                        entity.VideoCdnUrl = @event.FinalVideoUrl;
                        entity.LastFacility = EventNames.VideoGenerationResultPublished;

                        await customerContentRepository.UpdateAsync(entity, cancellationToken);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: EventNames.VideoGenerationResultPublished,
                            reference: new
                            {
                                entity.ScopeKey,
                                Type = nameof(CustomerContent),
                                Key = entity.Id,
                                RefType = "VideoRequest",
                                RefKey = @event.VideoRequestId,
                                entity.VideoCdnUrl
                            },
                            facility: Facilities.VideoGenerationResultPublished,
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));
                    }

                    break;
                }
            case ContentType.AnalysisContent:
                {
                    var entity = await analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);
                    if (entity != null)
                    {
                        entity.VideoStatus = MediaStatusNames.Completed;
                        entity.VideoRequestId = @event.VideoRequestId;
                        entity.VideoCdnUrl = @event.FinalVideoUrl;
                        entity.LastFacility = EventNames.VideoGenerationResultPublished;

                        await analysisContentRepository.UpdateAsync(entity, cancellationToken);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: EventNames.VideoGenerationResultPublished,
                            reference: new
                            {
                                entity.ScopeKey,
                                Type = nameof(AnalysisContent),
                                Key = entity.Id,
                                RefType = "VideoRequest",
                                RefKey = @event.VideoRequestId,
                                entity.VideoCdnUrl
                            },
                            facility: Facilities.VideoGenerationResultPublished,
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));
                    }

                    break;
                }
            case ContentType.None:
                break;
            default:
                throw new InvalidOperationException("Unknown content reference type for result update operation");
        }
    }

    public async Task HandleStepFailedAsync(StepFailedEto @event, CancellationToken cancellationToken = default)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

            if (entity is not null)
            {
                string facility = @event.Retryable ? Facilities.RetryScheduled : Facilities.StepFailed;
                string failedDesc = $"{@event.Step}: {@event.ErrorMessage}";

                entity.LastFacility = facility;
                entity.LastError = failedDesc;

                if (!@event.Retryable)
                {
                    if (IsNormalizeStep(@event.Step))
                        entity.NormalizeStatus = NormalizeStatusNames.Failed;

                    if (IsVideoStep(@event.Step))
                        entity.VideoStatus = MediaStatusNames.Failed;
                }

                await customerContentRepository.UpdateAsync(entity, cancellationToken);

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: $"{facility} | {failedDesc}",
                    reference: new
                    {
                        entity.ScopeKey,
                        // references
                        Type = nameof(CustomerContent),
                        Key = entity.Id,
                        FailedStep = @event.Step,
                        @event.Retryable,
                        @event.ErrorMessage
                    },
                    facility: facility,
                    correlationId: entity.CorrelationId,
                    exception: new Exception(failedDesc)
                ));
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);

            if (entity is not null)
            {
                string facility = @event.Retryable ? Facilities.RetryScheduled : Facilities.StepFailed;
                string failedDesc = $"{@event.Step}: {@event.ErrorMessage}";

                entity.LastFacility = facility;
                entity.LastError = failedDesc;

                if (!@event.Retryable)
                {
                    if (IsNormalizeStep(@event.Step))
                        entity.NormalizeStatus = NormalizeStatusNames.Failed;

                    if (IsVideoStep(@event.Step))
                        entity.VideoStatus = MediaStatusNames.Failed;
                }

                await analysisContentRepository.UpdateAsync(entity, cancellationToken);

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: $"{EventNames.StepFailed} | {failedDesc}",
                    reference: new
                    {
                        entity.ScopeKey,
                        // references
                        Type = nameof(AnalysisContent),
                        Key = entity.Id,
                        FailedStep = @event.Step,
                        @event.Retryable,
                        @event.ErrorMessage
                    },
                    facility: facility,
                    correlationId: entity.CorrelationId,
                    exception: new Exception(failedDesc)
                ));
            }
        }
    }

    private static bool IsNormalizeStep(string step)
    {
        return step.Contains(StepKeywords.Scraping, StringComparison.OrdinalIgnoreCase)
               || step.Contains(StepKeywords.Outline, StringComparison.OrdinalIgnoreCase)
               || step.Contains(StepKeywords.Normalize, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVideoStep(string step)
    {
        return step.Contains(StepKeywords.Audio, StringComparison.OrdinalIgnoreCase)
               || step.Contains(StepKeywords.Video, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<KeyValuePair<bool, string>> CheckVideoGenerationApproveRules([NotNull] string scopeKey, DateTime? releaseTime)
    {
        if (_serviceSettings.SkipContentCheckOperation)
        {
            return new KeyValuePair<bool, string>(true, EventNames.CustomerContentVideoGenerationApproved);
        }

        // Check content release time
        if (!releaseTime.HasValue || releaseTime.Value.ToUniversalTime().Date != DateTime.UtcNow.Date)
        {
            return new KeyValuePair<bool, string>(false, EventNames.CustomerContentVideoGenerationSkippedOldContent);
        }

        // Get Client Details
        var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == scopeKey);
        if (customerVpSetting == null)
        {
            throw new CustomerVpSettingNotFoundException(L, scopeKey);
        }

        // Check client video generation started settings
        if (DateTime.UtcNow.Hour < customerVpSetting.DailyDirectVideoGenerationStartedUtcHour)
        {
            if (DateTime.UtcNow.Hour < releaseTime.Value.ToUniversalTime().Hour)
            {
                return new KeyValuePair<bool, string>(false, EventNames.CustomerContentVideoGenerationSkippedEarlyTime);
            }
        }

        // Check client direct video generation limit
        if (customerVpSetting.DailyDirectVideoGenerationLimit <= 0)
        {
            return new KeyValuePair<bool, string>(false, EventNames.CustomerContentVideoGenerationSkippedDailyLimit);
        }

        // Check client direct video generation available
        long clientDailyDirectVideoHistoryCount = await contentVideoGenerationLimitRepository.GetCustomerVideoHistoryCountAsync(scopeKey,
            releaseTime.Value.ToUniversalTime().Date, VideoGenerationTypes.DirectVideoGeneration);

        return customerVpSetting.DailyDirectVideoGenerationLimit - clientDailyDirectVideoHistoryCount <= 0
            ? new KeyValuePair<bool, string>(false, EventNames.CustomerContentVideoGenerationSkippedDailyLimit)
            : new KeyValuePair<bool, string>(true, EventNames.CustomerContentVideoGenerationApproved);
    }
}