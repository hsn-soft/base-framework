using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Tracing;
using Microsoft.Extensions.Logging;

namespace Hhs.ContentService.Application.Services;

public sealed record CreateCustomerContentRequest(string ScopeKey, string DomainName, string ContentKey);

public sealed record CreateContentResponse(string Id);

public sealed record CreateAnalysisContentRequest(
    string ScopeKey,
    string DomainName,
    string Title,
    List<Guid> CustomerContentIds);

public sealed record CreateAnalysisContentFromScopeRequest(
    string ScopeKey,
    string DomainName,
    string Title,
    int MaxCustomerContents = 5);

public sealed class ContentOperationAppService(
    IServiceProvider provider,
    ITraceAccesor traceAccessor,
    ICustomerContentRepository customerContentRepository,
    IAnalysisContentRepository analysisContentRepository,
    ILogger<ContentOperationAppService> logger) : ApplicationServiceBase(provider)
{
    public async Task<CreateContentResponse> CreateCustomerContentAsync(CreateCustomerContentRequest request, CancellationToken cancellationToken)
    {
        // Add appContent record
        var placedCustomerContent = await customerContentRepository.CreateAsync(
            scopeKey: request.ScopeKey,
            contentKey: request.ContentKey,
            correlationId: traceAccessor?.GetCorrelationId());

        logger.LogInformation($"About to publish CustomerContentCreatedEto for RefContentId: {placedCustomerContent.Id}, ScopeKey: {placedCustomerContent.ScopeKey}");

        try
        {
            // Integration Event for TextNormalizerService
            await EventBus.PublishAsync(
                correlationId: placedCustomerContent.CorrelationId,
                eventMessage: new CustomerContentCreatedEto { CustomerContentId = placedCustomerContent.Id, ScopeKey = placedCustomerContent.ScopeKey, DomainName = request.DomainName, DomainPath = request.ContentKey }
            );

            logger.LogInformation($"Successfully published CustomerContentCreatedEto");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to publish CustomerContentCreatedEto: {ex.Message}");
            throw;
        }

        return new CreateContentResponse(Id: placedCustomerContent.Id.ToString());
    }

    public async Task<CreateContentResponse> CreateAnalysisContentAsync(CreateAnalysisContentRequest request, CancellationToken cancellationToken)
    {
        var analysisId = Guid.NewGuid();

        // Fetch all customer contents by IDs
        var contentsForAnalysis = new List<CustomerContent>();
        foreach (var id in request.CustomerContentIds)
        {
            var content = await customerContentRepository.GetByIdWithTrackingAsync(id, cancellationToken);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {id}");
            contentsForAnalysis.Add(content);
        }

        var analysis = new AnalysisContent(
            analysisId,
            request.ScopeKey,
            request.DomainName,
            request.Title,
            traceAccessor?.GetCorrelationId());

        int sort = 1;

        foreach (var customerContentId in request.CustomerContentIds)
        {
            var content = contentsForAnalysis.FirstOrDefault(x => x.Id == customerContentId);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

            analysis.Items.Add(new AnalysisContentItem(id: Guid.NewGuid()
                , analysisContentId: analysisId, customerContentId: customerContentId, sortOrder: sort++));
        }

        await analysisContentRepository.InsertAsync(analysis, cancellationToken);

        var items = request.CustomerContentIds
            .Select((id, index) =>
            {
                var content = contentsForAnalysis.First(x => x.Id == id);

                return new AnalysisNormalizeItem { CustomerContentId = content.Id, ContentKey = content.ContentKey, SortOrder = index + 1 };
            })
            .ToList();

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: analysis.CorrelationId,
            eventMessage: new AnalysisContentCreatedEto { AnalysisContentId = analysisId, ScopeKey = analysis.ScopeKey, DomainName = analysis.DomainName, Items = items }
        );

        return new CreateContentResponse(Id: analysisId.ToString());
    }

    public async Task<CreateContentResponse> CreateAnalysisContentFromScopeAsync(CreateAnalysisContentFromScopeRequest request, CancellationToken cancellationToken)
    {
        // Fetch latest N successful CustomerContents with this ScopeKey and COMPLETED NormalizeStatus
        var options = new ListQueryOptions<CustomerContent> { Filter = x => x.ScopeKey == request.ScopeKey && x.NormalizeStatus == StatusNames.Completed, OrderByEntity = o => o.OrderByDescending(s => s.CreationTime), MaxResultCount = request.MaxCustomerContents };

        var successfulContents = await customerContentRepository.GetListAsync(options, cancellationToken);

        if (successfulContents.Count == 0)
            throw new InvalidOperationException($"No successful CustomerContent found for ScopeKey: {request.ScopeKey}");

        // Extract IDs for analysis
        var customerContentIds = successfulContents.Select(x => x.Id).ToList();

        // Create analysis using extracted IDs
        var analysisId = Guid.NewGuid();

        var analysis = new AnalysisContent(
            analysisId,
            request.ScopeKey,
            request.DomainName,
            request.Title,
            traceAccessor?.GetCorrelationId());

        int sort = 1;

        foreach (var customerContentId in customerContentIds)
        {
            var content = successfulContents.FirstOrDefault(x => x.Id == customerContentId);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

            analysis.Items.Add(new AnalysisContentItem(id: Guid.NewGuid()
                , analysisContentId: analysisId, customerContentId: customerContentId, sortOrder: sort++));
        }

        await analysisContentRepository.InsertAsync(analysis, cancellationToken);

        var items = customerContentIds
            .Select((id, index) =>
            {
                var content = successfulContents.First(x => x.Id == id);

                return new AnalysisNormalizeItem { CustomerContentId = content.Id, ContentKey = content.ContentKey, SortOrder = index + 1 };
            })
            .ToList();

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: analysis.CorrelationId,
            eventMessage: new AnalysisContentCreatedEto { AnalysisContentId = analysisId, ScopeKey = analysis.ScopeKey, DomainName = analysis.DomainName, Items = items }
        );

        return new CreateContentResponse(Id: analysisId.ToString());
    }

    public async Task HandleNormalizeStartedAsync(Guid contentId, Guid normalizeRequestId, ContentType contentType, CancellationToken cancellationToken = default)
    {
        if (contentType == ContentType.CustomerContent)
        {
            var entity = await customerContentRepository.GetByIdWithTrackingAsync(contentId, cancellationToken);
            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = normalizeRequestId;
                entity.NormalizeStatus = StatusNames.Created;
                entity.LastFacility = EventNames.CustomerContentNormalizeRequestCreated;
                await customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }
        else if (contentType == ContentType.AnalysisContent)
        {
            var entity = await analysisContentRepository.GetByIdWithItemsAsync(contentId, cancellationToken);
            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = normalizeRequestId;
                entity.NormalizeStatus = StatusNames.Created;
                entity.LastFacility = EventNames.AnalysisContentNormalizeRequestCreated;
                await analysisContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }
    }

    public async Task HandleScrapingCompletedAsync(Guid customerContentId, DateTime? scrapReleaseTimeUtc, CancellationToken cancellationToken = default)
    {
        var entity = await customerContentRepository.GetByIdWithTrackingAsync(customerContentId, cancellationToken);
        if (entity != null && entity.ScrapReleaseTimeUtc == null)
        {
            entity.SetScrapReleaseTimeUtc(scrapReleaseTimeUtc);

            await customerContentRepository.UpdateAsync(entity, cancellationToken);
        }
    }

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
                entity.FinalVideoUrl = @event.FinalVideoUrl;
                entity.LastFacility = EventNames.VideoGenerationResultPublished;

                await analysisContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }
    }


    public async Task HandleVideoRequestCreatedAsync(Guid contentId, Guid videoRequestId, ContentType contentType, CancellationToken cancellationToken = default)
    {
        if (contentType == ContentType.CustomerContent)
        {
            var entity = await customerContentRepository.GetByIdWithTrackingAsync(contentId, cancellationToken);
            if (entity != null && entity.VideoRequestId == null)
            {
                entity.VideoRequestId = videoRequestId;
                entity.VideoStatus = StatusNames.Created;
                entity.LastFacility = EventNames.VideoRequestCreated;
                await customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }
        else if (contentType == ContentType.AnalysisContent)
        {
            var entity = await analysisContentRepository.GetByIdWithItemsAsync(contentId, cancellationToken);
            if (entity != null && entity.VideoRequestId == null)
            {
                entity.VideoRequestId = videoRequestId;
                entity.VideoStatus = StatusNames.Created;
                entity.LastFacility = EventNames.VideoRequestCreated;
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