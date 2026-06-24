using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Text;
using HsnSoft.Base.Tracing;
using Microsoft.EntityFrameworkCore;
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
    ContentServiceDbContext db,
    ILogger<ContentOperationAppService> logger) : ApplicationServiceBase(provider)
{
    public async Task<CreateContentResponse> CreateCustomerContentAsync(CreateCustomerContentRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        var entity = new CustomerContent
        {
            Id = id,
            ScopeKey = request.ScopeKey,
            DomainName = request.DomainName,
            ContentKey = request.ContentKey,
            SlugKey = StringHelper.SlugKeyNormalize(request.ContentKey),
            NormalizeStatus = StatusNames.Created,
            VideoStatus = StatusNames.NotStarted,
            LastFacility = EventNames.CustomerContentCreated,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CorrelationId = traceAccessor?.GetCorrelationId()
        };

        db.CustomerContents.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

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

        var contents = await db.CustomerContents
            .Where(x => request.CustomerContentIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var contentMap = contents.ToDictionary(x => x.Id);

        var analysis = new AnalysisContent
        {
            Id = analysisId,
            ScopeKey = request.ScopeKey,
            DomainName = request.DomainName,
            Title = request.Title,
            NormalizeStatus = StatusNames.Created,
            VideoStatus = StatusNames.NotStarted,
            LastFacility = EventNames.AnalysisContentCreated,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CorrelationId = traceAccessor?.GetCorrelationId()
        };

        int sort = 1;

        foreach (var customerContentId in request.CustomerContentIds)
        {
            if (!contentMap.ContainsKey(customerContentId))
                throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

            analysis.Items.Add(new AnalysisContentItem { Id = Guid.NewGuid(), AnalysisContentId = analysisId, CustomerContentId = customerContentId, SortOrder = sort++ });
        }

        db.AnalysisContents.Add(analysis);
        await db.SaveChangesAsync(cancellationToken);

        var items = request.CustomerContentIds
            .Select((id, index) =>
            {
                var content = contentMap[id];

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

    public async Task HandleNormalizerResultAsync(NormalizerResultPublishedEto @event)
    {
        bool shouldPublishEvent = false;

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await db.CustomerContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId);

            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = @event.NormalizeRequestId;
                entity.NormalizeStatus = StatusNames.Completed;
                entity.VideoStatus = StatusNames.Approved;
                entity.LastFacility = EventNames.NormalizerResultPublished;
                entity.LastError = null;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                shouldPublishEvent = true;
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await db.AnalysisContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId);

            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = @event.NormalizeRequestId;
                entity.NormalizeStatus = StatusNames.Completed;
                entity.VideoStatus = StatusNames.Approved;
                entity.LastFacility = EventNames.NormalizerResultPublished;
                entity.LastError = null;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                shouldPublishEvent = true;
            }
        }

        if (shouldPublishEvent)
        {
            await db.SaveChangesAsync();

            string? scopeKey = @event.RefContentType == ContentType.CustomerContent
                ? (await db.CustomerContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId))?.ScopeKey
                : (await db.AnalysisContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId))?.ScopeKey;

            await EventBus.PublishAsync(
                parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationApprovedEto { RefContentId = @event.RefContentId, RefContentType = @event.RefContentType, ScopeKey = scopeKey ?? string.Empty, VideoInputJson = @event.VideoInputJson }
            );
        }
    }

    public async Task HandleVideoResultAsync(VideoGenerationResultPublishedEto @event)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await db.CustomerContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId);
            if (entity != null)
            {
                entity.VideoStatus = StatusNames.Completed;
                entity.VideoRequestId = @event.VideoRequestId;
                entity.FinalVideoUrl = @event.FinalVideoUrl;
                entity.LastFacility = EventNames.VideoGenerationResultPublished;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await db.AnalysisContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId);
            if (entity != null)
            {
                entity.VideoStatus = StatusNames.Completed;
                entity.VideoRequestId = @event.VideoRequestId;
                entity.FinalVideoUrl = @event.FinalVideoUrl;
                entity.LastFacility = EventNames.VideoGenerationResultPublished;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task HandleStepFailedAsync(StepFailedEto @event)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await db.CustomerContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId);

            if (entity is not null)
            {
                entity.LastFacility = EventNames.StepFailed;
                entity.LastError = @event.ErrorMessage;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                if (!@event.Retryable)
                {
                    if (IsNormalizeStep(@event.Step))
                        entity.NormalizeStatus = StatusNames.Failed;

                    if (IsVideoStep(@event.Step))
                        entity.VideoStatus = StatusNames.Failed;
                }
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await db.AnalysisContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId);

            if (entity is not null)
            {
                entity.LastFacility = EventNames.StepFailed;
                entity.LastError = @event.ErrorMessage;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                if (!@event.Retryable)
                {
                    if (IsNormalizeStep(@event.Step))
                        entity.NormalizeStatus = StatusNames.Failed;

                    if (IsVideoStep(@event.Step))
                        entity.VideoStatus = StatusNames.Failed;
                }
            }
        }

        await db.SaveChangesAsync();
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