using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;

namespace Hhs.ContentService.Application.Services;

public sealed class ContentOperationService(
    IServiceProvider provider,
    ICustomerContentRepository customerContentRepository,
    IAnalysisContentRepository analysisContentRepository) : ApplicationServiceBase(provider)
{
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

        if (@event.RefContentType is not (ContentType.CustomerContent or ContentType.AnalysisContent))
        {
            var entity = await customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

            if (entity != null && entity.NormalizeStatus != StatusNames.Completed)
            {
                entity.NormalizeStatus = StatusNames.Completed;
                entity.LastFacility = EventNames.NormalizerResultPublished;
                entity.LastError = "Unknown content reference type for approve operation";

                await customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

            if (entity != null && entity.NormalizeStatus != StatusNames.Completed)
            {
                entity.NormalizeStatus = StatusNames.Completed;
                entity.LastFacility = EventNames.NormalizerResultPublished;
                entity.LastError = null;
                scopeKey = entity.ScopeKey;

                entity.VideoStatus = StatusNames.Approved;
                shouldPublishEvent = true;

                await customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);

            if (entity != null && entity.NormalizeStatus != StatusNames.Completed)
            {
                entity.NormalizeStatus = StatusNames.Completed;
                entity.LastFacility = EventNames.NormalizerResultPublished;
                entity.LastError = null;
                scopeKey = entity.ScopeKey;

                entity.VideoStatus = StatusNames.Approved;
                shouldPublishEvent = true;

                await analysisContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (shouldPublishEvent)
        {
            await EventBus.PublishAsync(
                parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationApprovedEto { RefContentId = @event.RefContentId, RefContentType = @event.RefContentType, ScopeKey = scopeKey ?? string.Empty, RefNormalizeRequestId = @event.NormalizeRequestId }
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
        if (@event.RefContentType == ContentType.CustomerContent)
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
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
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
}