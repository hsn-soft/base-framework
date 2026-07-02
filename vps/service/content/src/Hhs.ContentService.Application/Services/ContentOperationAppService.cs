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

    public async Task HandleNormalizedRequestReferenceAsync(ContentType refContentType, Guid refContentId, Guid refNormalizeRequestId)
    {
        switch (refContentType)
        {
            case ContentType.CustomerContent:
                await customerContentRepository.SetNormalizedReferenceAsync(refContentId, refNormalizeRequestId);
                break;
            case ContentType.AnalysisContent:
                await analysisContentRepository.SetNormalizedReferenceAsync(refContentId, refNormalizeRequestId);
                break;
            case ContentType.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(refContentType), refContentType, null);
        }
    }

    public async Task HandleCustomerContentScrapeTimeAsync(Guid customerContentId, DateTime? scrapedReleaseTimeUtc)
        => await customerContentRepository.SetScrapeTimeAsync(customerContentId, scrapedReleaseTimeUtc);

    public async Task HandleNormalizerResultAsync(NormalizerResultPublishedEto @event, CancellationToken cancellationToken = default)
    {
        bool shouldPublishEvent = false;
        string? scopeKey = null;

        switch (@event.RefContentType)
        {
            case ContentType.CustomerContent:
                {
                    var entity = await customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

                    if (entity != null && entity.NormalizeStatus != StatusNames.Completed)
                    {
                        if (@event.NormalizeStatus == StatusNames.OutlineSkipped)
                        {
                            _logger.FrameworkInfoLog(LogHelper.Generate(
                                message: "CustomerContent normalized skipped",
                                reference: new { entity.ScopeKey, RefContentId = entity.Id, RefNormalizedRequestId = entity.NormalizeRequestId },
                                facility: "CUSTOMER_CONTENT_NORMALIZED_SKIPPED",
                                correlationId: entity.CorrelationId,
                                exception: null
                            ));

                            await customerContentRepository.SetVideoGenerationRejectedAsync(entity.Id, "CUSTOMER_CONTENT_OUTLINE_SKIPPED");

                            _logger.FrameworkInfoLog(LogHelper.Generate(
                                message: "CustomerContent video generation rejected",
                                reference: new { entity.ScopeKey, RefContentId = entity.Id, RefNormalizedRequestId = entity.NormalizeRequestId },
                                facility: "CUSTOMER_CONTENT_OUTLINE_SKIPPED",
                                correlationId: entity.CorrelationId,
                                exception: null
                            ));

                            return;
                        }

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: "CustomerContent normalized success",
                            reference: new { entity.ScopeKey, RefContentId = entity.Id, RefNormalizedRequestId = entity.NormalizeRequestId },
                            facility: "CUSTOMER_CONTENT_NORMALIZED_SUCCESS",
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));

                        var checkResult = await CheckVideoGenerationApproveRules(entity.ScopeKey, entity.ScrapReleaseTimeUtc);
                        if (checkResult.Key)
                        {
                            await customerContentRepository.SetVideoGenerationApprovedAsync(id: entity.Id);

                            _logger.FrameworkInfoLog(LogHelper.Generate(
                                message: "CustomerContent video generation approved",
                                reference: new { entity.ScopeKey, RefContentId = entity.Id, RefNormalizedRequestId = entity.NormalizeRequestId },
                                facility: "CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED",
                                correlationId: entity.CorrelationId,
                                exception: null
                            ));

                            // Add video generation history for client quote control
                            await contentVideoGenerationLimitRepository.CreateAsync(scopeKey: entity.ScopeKey,
                                videoGenerationDate: entity.ScrapReleaseTimeUtc?.Date ?? DateTime.UtcNow.Date,
                                videoGenerationType: VideoGenerationTypes.DirectVideoGeneration,
                                contentReferenceIds: entity.Id.ToString());

                            scopeKey = entity.ScopeKey;
                            shouldPublishEvent = true;
                        }
                        else
                        {
                            await customerContentRepository.SetVideoGenerationRejectedAsync(entity.Id, checkResult.Value);

                            _logger.FrameworkInfoLog(LogHelper.Generate(
                                message: "CustomerContent video generation rejected",
                                reference: new { entity.ScopeKey, RefContentId = entity.Id, RefNormalizedRequestId = entity.NormalizeRequestId },
                                facility: checkResult.Value,
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

                    if (entity != null && entity.NormalizeStatus != StatusNames.Completed)
                    {
                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: "Analysis Content normalized success",
                            reference: new { entity.ScopeKey, RefContentId = entity.Id, RefNormalizedRequestId = entity.NormalizeRequestId },
                            facility: "ANALYSIS_CONTENT_NORMALIZED_SUCCESS",
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));

                        await analysisContentRepository.SetVideoGenerationApprovedAsync(id: entity.Id);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: "Analysis Content video generation approved",
                            reference: new { entity.ScopeKey, RefContentId = entity.Id, RefNormalizedRequestId = entity.NormalizeRequestId },
                            facility: "ANALYSIS_CONTENT_VIDEO_GENERATION_APPROVED",
                            correlationId: entity.CorrelationId,
                            exception: null
                        ));

                        // Add video generation history for client quote control
                        await contentVideoGenerationLimitRepository.CreateAsync(scopeKey: entity.ScopeKey,
                            videoGenerationDate: entity.AnalysisDate.Date,
                            videoGenerationType: VideoGenerationTypes.AnalysisVideoGeneration,
                            contentReferenceIds: entity.Id.ToString());

                        scopeKey = entity.ScopeKey;
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

    public async Task HandleVideoRequestCreatedAsync(Guid refContentId, Guid refVideoRequestId, ContentType refContentType, CancellationToken cancellationToken = default)
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
                        entity.VideoStatus = StatusNames.Completed;
                        entity.VideoRequestId = @event.VideoRequestId;
                        entity.VideoCdnUrl = @event.FinalVideoUrl;
                        entity.LastFacility = EventNames.VideoGenerationResultPublished;

                        await customerContentRepository.UpdateAsync(entity, cancellationToken);
                    }

                    break;
                }
            case ContentType.AnalysisContent:
                {
                    var entity = await analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);
                    if (entity != null)
                    {
                        entity.VideoStatus = StatusNames.Completed;
                        entity.VideoRequestId = @event.VideoRequestId;
                        entity.VideoCdnUrl = @event.FinalVideoUrl;
                        entity.LastFacility = EventNames.VideoGenerationResultPublished;

                        await analysisContentRepository.UpdateAsync(entity, cancellationToken);
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
                entity.LastFacility = EventNames.StepFailed;
                entity.LastError = @event.ErrorMessage;

                if (!@event.Retryable)
                {
                    if (IsNormalizeStep(@event.Step))
                        entity.NormalizeStatus = StatusNames.Failed;

                    if (IsVideoStep(@event.Step))
                        entity.VideoStatus = StatusNames.Failed;
                }

                await customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);

            if (entity is not null)
            {
                entity.LastFacility = EventNames.StepFailed;
                entity.LastError = @event.ErrorMessage;

                if (!@event.Retryable)
                {
                    if (IsNormalizeStep(@event.Step))
                        entity.NormalizeStatus = StatusNames.Failed;

                    if (IsVideoStep(@event.Step))
                        entity.VideoStatus = StatusNames.Failed;
                }

                await analysisContentRepository.UpdateAsync(entity, cancellationToken);
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
            return new KeyValuePair<bool, string>(true, "CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED");
        }

        // Check content release time
        if (!releaseTime.HasValue || releaseTime.Value.ToUniversalTime().Date != DateTime.UtcNow.Date)
        {
            return new KeyValuePair<bool, string>(false, "CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_OLD_CONTENT");
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
                return new KeyValuePair<bool, string>(false, "CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_EARLY_TIME");
            }
        }

        // Check client direct video generation limit
        if (customerVpSetting.DailyDirectVideoGenerationLimit <= 0)
        {
            return new KeyValuePair<bool, string>(false, "CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_DAILY_LIMIT");
        }

        // Check client direct video generation available
        long clientDailyDirectVideoHistoryCount = await contentVideoGenerationLimitRepository.GetCustomerVideoHistoryCountAsync(scopeKey,
            releaseTime.Value.ToUniversalTime().Date, VideoGenerationTypes.DirectVideoGeneration);

        return customerVpSetting.DailyDirectVideoGenerationLimit - clientDailyDirectVideoHistoryCount <= 0
            ? new KeyValuePair<bool, string>(false, "CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_DAILY_LIMIT")
            : new KeyValuePair<bool, string>(true, "CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED");
    }
}