using Hhs.ContentService.Data;
using Hhs.ContentService.Entities;
using Hhs.Shared.Events;
using Hhs.Shared.Helpers;
using Hhs.Shared.RabbitMQ;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.Services;

public sealed class ContentOperationAppService
{
    private readonly ContentDbContext _db;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ContentOperationAppService> _logger;

    public ContentOperationAppService(
        ContentDbContext db,
        IEventBus eventBus,
        ILogger<ContentOperationAppService> logger)
    {
        _db = db;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Guid> CreateCustomerContentAsync(
        CreateCustomerContentRequest request,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        var entity = new CustomerContent
        {
            Id = id,
            ScopeKey = request.ScopeKey,
            DomainName = request.DomainName,
            ContentKey = request.ContentKey,
            SlugKey = StringHelper.ToSlug(request.ContentKey),
            NormalizeStatus = StatusNames.Created,
            VideoStatus = StatusNames.NotStarted,
            LastFacility = EventNames.CustomerContentCreated,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        _db.CustomerContents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"About to publish CustomerContentCreatedEto for RefContentId: {id}, ScopeKey: {entity.ScopeKey}");

        try
        {
            await _eventBus.PublishAsync(new CustomerContentCreatedEto
            {
                RefContentId = id,
                CorrelationId = entity.CorrelationId,
                ScopeKey = entity.ScopeKey,
                DomainName = entity.DomainName,
                ContentKey = entity.ContentKey
            }, cancellationToken);

            _logger.LogInformation($"Successfully published CustomerContentCreatedEto");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish CustomerContentCreatedEto: {ex.Message}");
            throw;
        }

        return id;
    }

    public async Task<Guid> CreateAnalysisContentAsync(
        CreateAnalysisContentRequest request,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var analysisId = Guid.NewGuid();

        var contents = await _db.CustomerContents
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
            CorrelationId = correlationId
        };

        int sort = 1;

        foreach (var customerContentId in request.CustomerContentIds)
        {
            if (!contentMap.ContainsKey(customerContentId))
                throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

            analysis.Items.Add(new AnalysisContentItem { Id = Guid.NewGuid(), AnalysisContentId = analysisId, CustomerContentId = customerContentId, SortOrder = sort++ });
        }

        _db.AnalysisContents.Add(analysis);
        await _db.SaveChangesAsync(cancellationToken);

        var items = request.CustomerContentIds
            .Select((id, index) =>
            {
                var content = contentMap[id];

                return new AnalysisNormalizeItem { CustomerContentId = content.Id, ContentKey = content.ContentKey, SortOrder = index + 1 };
            })
            .ToList();

        await _eventBus.PublishAsync(new AnalysisContentCreatedEto
        {
            RefContentId = analysisId,
            CorrelationId = analysis.CorrelationId,
            ScopeKey = analysis.ScopeKey,
            DomainName = analysis.DomainName,
            Items = items
        }, cancellationToken);

        return analysisId;
    }

    public async Task HandleNormalizerResultAsync(
        NormalizerResultPublishedEto @event,
        CancellationToken cancellationToken)
    {
        bool shouldPublishEvent = false;

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await _db.CustomerContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken);

            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = @event.NormalizeRequestId;
                entity.NormalizeStatus = StatusNames.Completed;
                entity.VideoStatus = StatusNames.Approved;
                entity.LastFacility = @event.Facility;
                entity.LastError = null;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                shouldPublishEvent = true;
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await _db.AnalysisContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken);

            if (entity != null && entity.NormalizeRequestId == null)
            {
                entity.NormalizeRequestId = @event.NormalizeRequestId;
                entity.NormalizeStatus = StatusNames.Completed;
                entity.VideoStatus = StatusNames.Approved;
                entity.LastFacility = @event.Facility;
                entity.LastError = null;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                shouldPublishEvent = true;
            }
        }

        if (shouldPublishEvent)
        {
            await _db.SaveChangesAsync(cancellationToken);

            string? scopeKey = @event.RefContentType == ContentType.CustomerContent
                ? (await _db.CustomerContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken))?.ScopeKey
                : (await _db.AnalysisContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken))?.ScopeKey;

            await _eventBus.PublishAsync(new VideoGenerationApprovedEto
            {
                RefContentId = @event.RefContentId,
                RefContentType = @event.RefContentType,
                ScopeKey = scopeKey ?? string.Empty,
                VideoInputJson = @event.VideoInputJson,
                CorrelationId = @event.CorrelationId,
            }, cancellationToken);
        }
    }

    public async Task HandleVideoResultAsync(VideoGenerationResultPublishedEto @event, CancellationToken cancellationToken)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await _db.CustomerContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken);
            if (entity != null)
            {
                entity.VideoStatus = StatusNames.Completed;
                entity.VideoRequestId = @event.VideoRequestId;
                entity.FinalVideoUrl = @event.FinalVideoUrl;
                entity.LastFacility = @event.Facility;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var entity = await _db.AnalysisContents.FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken);
            if (entity != null)
            {
                entity.VideoStatus = StatusNames.Completed;
                entity.VideoRequestId = @event.VideoRequestId;
                entity.FinalVideoUrl = @event.FinalVideoUrl;
                entity.LastFacility = @event.Facility;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStepFailedAsync(
        StepFailedEto @event,
        CancellationToken cancellationToken)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var entity = await _db.CustomerContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken);

            if (entity is not null)
            {
                entity.LastFacility = @event.Facility;
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
            var entity = await _db.AnalysisContents
                .FirstOrDefaultAsync(x => x.Id == @event.RefContentId, cancellationToken);

            if (entity is not null)
            {
                entity.LastFacility = @event.Facility;
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

        await _db.SaveChangesAsync(cancellationToken);
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