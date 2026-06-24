using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Text;
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

public sealed class ContentOperationAppService(
    IServiceProvider provider,
    ITraceAccesor traceAccessor,
    ICustomerContentRepository customerContentRepository,
    IAnalysisContentRepository analysisContentRepository,
    ILogger<ContentOperationAppService> logger) : ApplicationServiceBase(provider)
{
    private readonly ICustomerContentRepository _customerContentRepository = customerContentRepository;
    private readonly IAnalysisContentRepository _analysisContentRepository = analysisContentRepository;

    public async Task<CreateContentResponse> CreateCustomerContentAsync(CreateCustomerContentRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var slugKey = StringHelper.SlugKeyNormalize(request.ContentKey);

        var entity = new CustomerContent(
            id,
            request.ScopeKey,
            request.DomainName,
            request.ContentKey,
            slugKey,
            traceAccessor?.GetCorrelationId());

        await _customerContentRepository.InsertAsync(entity, cancellationToken);

        logger.LogInformation($"About to publish CustomerContentCreatedEto for RefContentId: {id}, ScopeKey: {entity.ScopeKey}");

        try
        {
            await EventBus.PublishAsync(
                parentMessage: ParentIntegrationEvent,
                correlationId: entity.CorrelationId,
                eventMessage: new CustomerContentCreatedEto { CustomerContentId = id, ScopeKey = entity.ScopeKey, DomainName = entity.DomainName, ContentKey = entity.ContentKey }
            );

            logger.LogInformation($"Successfully published CustomerContentCreatedEto");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to publish CustomerContentCreatedEto: {ex.Message}");
            throw;
        }

        return new CreateContentResponse(Id: id.ToString());
    }

    public async Task<CreateContentResponse> CreateAnalysisContentAsync(CreateAnalysisContentRequest request, CancellationToken cancellationToken)
    {
        var analysisId = Guid.NewGuid();

        // Fetch all customer contents by IDs
        var contentsForAnalysis = new List<CustomerContent>();
        foreach (var id in request.CustomerContentIds)
        {
            var content = await _customerContentRepository.GetByIdWithTrackingAsync(id, cancellationToken);
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

        await _analysisContentRepository.InsertAsync(analysis, cancellationToken);

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

    public async Task HandleNormalizerResultAsync(NormalizerResultPublishedEto @event, CancellationToken cancellationToken = default)
    {
        bool shouldPublishEvent = false;
        string? scopeKey = null;

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await _customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = @event.NormalizeRequestId;
                entity.NormalizeStatus = StatusNames.Completed;
                entity.VideoStatus = StatusNames.Approved;
                entity.LastFacility = EventNames.NormalizerResultPublished;
                entity.LastError = null;
                shouldPublishEvent = true;
                scopeKey = entity.ScopeKey;

                await _customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await _analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);

            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = @event.NormalizeRequestId;
                entity.NormalizeStatus = StatusNames.Completed;
                entity.VideoStatus = StatusNames.Approved;
                entity.LastFacility = EventNames.NormalizerResultPublished;
                entity.LastError = null;
                shouldPublishEvent = true;
                scopeKey = entity.ScopeKey;

                await _analysisContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (shouldPublishEvent)
        {
            await EventBus.PublishAsync(
                parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationApprovedEto { RefContentId = @event.RefContentId, RefContentType = @event.RefContentType, ScopeKey = scopeKey ?? string.Empty, VideoInputJson = @event.VideoInputJson }
            );
        }
    }

    public async Task HandleVideoResultAsync(VideoGenerationResultPublishedEto @event, CancellationToken cancellationToken = default)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await _customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);
            if (entity != null)
            {
                entity.VideoStatus = StatusNames.Completed;
                entity.VideoRequestId = @event.VideoRequestId;
                entity.FinalVideoUrl = @event.FinalVideoUrl;
                entity.LastFacility = EventNames.VideoGenerationResultPublished;

                await _customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await _analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);
            if (entity != null)
            {
                entity.VideoStatus = StatusNames.Completed;
                entity.VideoRequestId = @event.VideoRequestId;
                entity.FinalVideoUrl = @event.FinalVideoUrl;
                entity.LastFacility = EventNames.VideoGenerationResultPublished;

                await _analysisContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }
    }

    public async Task HandleStepFailedAsync(StepFailedEto @event, CancellationToken cancellationToken = default)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await _customerContentRepository.GetByIdWithTrackingAsync(@event.RefContentId, cancellationToken);

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

                await _customerContentRepository.UpdateAsync(entity, cancellationToken);
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await _analysisContentRepository.GetByIdWithItemsAsync(@event.RefContentId, cancellationToken);

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

                await _analysisContentRepository.UpdateAsync(entity, cancellationToken);
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