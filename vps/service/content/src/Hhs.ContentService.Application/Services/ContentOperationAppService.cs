using Hhs.ContentService.Domain.Constants;
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

    public async Task HandleNormalizedRequestReferenceAsync(ContentType refContentType, Guid refContentId, Guid refNormalizeRequestId, string normalizeStatus, string normalizeCurrentMilestone, [CanBeNull] string correlationId = null)
    {
        switch (refContentType)
        {
            case ContentType.CustomerContent:
                await customerContentRepository.SetNormalizedReferenceAsync(refContentId, refNormalizeRequestId, normalizeStatus, normalizeCurrentMilestone);
                break;
            case ContentType.AnalysisContent:
                await analysisContentRepository.SetNormalizedReferenceAsync(refContentId, refNormalizeRequestId, normalizeStatus, normalizeCurrentMilestone);
                break;
            case ContentType.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(refContentType), refContentType, null);
        }

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: Milestones.CustomerContentNormalizeRequestCreated,
            reference: new
            {
                Type = refContentType.ToString(),
                Key = refContentId,
                RefType = refContentType == ContentType.AnalysisContent
                    ? "AnalysisContentNormalizedRequest"
                    : "CustomerContentNormalizedRequest",
                RefKey = refNormalizeRequestId
            },
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
            @event.NormalizeCurrentMilestone,
            @event.ScrapedReleaseTimeUtc);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: Milestones.CustomerContentScrapingCompleted,
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
                                @event.NormalizeCurrentMilestone);

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

                        var checkResult = await CheckDirectVideoGenerationApproveRules(entity.ScopeKey, entity.ScrapReleaseTimeUtc);
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
            message: Milestones.VideoRequestCreated,
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
            message: Milestones.VideoProviderRequestStarted,
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
                        entity.LastFacility = Facilities.VideoGenerationResultPublished;
                        entity.CurrentMilestone = Milestones.VideoGenerationResultPublished;

                        await customerContentRepository.UpdateAsync(entity, cancellationToken);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: Milestones.VideoGenerationResultPublished,
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
                        entity.LastFacility = Facilities.VideoGenerationResultPublished;
                        entity.CurrentMilestone = Milestones.VideoGenerationResultPublished;

                        await analysisContentRepository.UpdateAsync(entity, cancellationToken);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: Milestones.VideoGenerationResultPublished,
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

    public async Task HandleMilestoneFailedAsync(MilestoneFailedEto @event, CancellationToken cancellationToken = default)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

            if (entity is not null)
            {
                string facility = @event.Retryable ? Facilities.RetryScheduled : Facilities.MilestoneFailed;
                string failedDesc = $"{@event.Milestone}: {@event.ErrorMessage}";

                entity.LastFacility = facility;
                entity.CurrentMilestone = @event.Milestone;
                entity.LastError = failedDesc;

                if (!@event.Retryable)
                {
                    if (IsNormalizeMilestone(@event.Milestone))
                        entity.NormalizeStatus = NormalizeStatusNames.Failed;

                    if (IsVideoMilestone(@event.Milestone))
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
                        FailedMilestone = @event.Milestone,
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
                string facility = @event.Retryable ? Facilities.RetryScheduled : Facilities.MilestoneFailed;
                string failedDesc = $"{@event.Milestone}: {@event.ErrorMessage}";

                entity.LastFacility = facility;
                entity.CurrentMilestone = @event.Milestone;
                entity.LastError = failedDesc;

                if (!@event.Retryable)
                {
                    if (IsNormalizeMilestone(@event.Milestone))
                        entity.NormalizeStatus = NormalizeStatusNames.Failed;

                    if (IsVideoMilestone(@event.Milestone))
                        entity.VideoStatus = MediaStatusNames.Failed;
                }

                await analysisContentRepository.UpdateAsync(entity, cancellationToken);

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: $"{Milestones.MilestoneFailed} | {failedDesc}",
                    reference: new
                    {
                        entity.ScopeKey,
                        // references
                        Type = nameof(AnalysisContent),
                        Key = entity.Id,
                        FailedMilestone = @event.Milestone,
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

    private static bool IsNormalizeMilestone(string milestone)
    {
        return milestone.Contains(MilestoneKeywords.Scraping, StringComparison.OrdinalIgnoreCase)
               || milestone.Contains(MilestoneKeywords.Outline, StringComparison.OrdinalIgnoreCase)
               || milestone.Contains(MilestoneKeywords.Normalize, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVideoMilestone(string milestone)
    {
        return milestone.Contains(MilestoneKeywords.Audio, StringComparison.OrdinalIgnoreCase)
               || milestone.Contains(MilestoneKeywords.Video, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<KeyValuePair<bool, string>> CheckDirectVideoGenerationApproveRules([NotNull] string scopeKey, DateTime? releaseTime)
    {
        if (_serviceSettings.SkipDirectVideoGenerationApproveRules)
        {
            return new KeyValuePair<bool, string>(true, Milestones.CustomerContentVideoGenerationApproved);
        }

        // Check content release time
        if (!releaseTime.HasValue || releaseTime.Value.ToUniversalTime().Date != DateTime.UtcNow.Date)
        {
            return new KeyValuePair<bool, string>(false, Milestones.CustomerContentVideoGenerationSkippedOldContent);
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
                return new KeyValuePair<bool, string>(false, Milestones.CustomerContentVideoGenerationSkippedEarlyTime);
            }
        }

        // Check client direct video generation limit
        if (customerVpSetting.DailyDirectVideoGenerationLimit <= 0)
        {
            return new KeyValuePair<bool, string>(false, Milestones.CustomerContentVideoGenerationSkippedDailyLimit);
        }

        // Check client direct video generation available
        long clientDailyDirectVideoHistoryCount = await contentVideoGenerationLimitRepository.GetCustomerVideoHistoryCountAsync(scopeKey,
            releaseTime.Value.ToUniversalTime().Date, VideoGenerationTypes.DirectVideoGeneration);

        return customerVpSetting.DailyDirectVideoGenerationLimit - clientDailyDirectVideoHistoryCount <= 0
            ? new KeyValuePair<bool, string>(false, Milestones.CustomerContentVideoGenerationSkippedDailyLimit)
            : new KeyValuePair<bool, string>(true, Milestones.CustomerContentVideoGenerationApproved);
    }
}