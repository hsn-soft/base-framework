using Hhs.ContentService.Data;
using Hhs.ContentService.Entities;
using Hhs.Shared.Events;
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
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        var entity = new CustomerContent
        {
            Id = id,
            Url = request.Url,
            NormalizeStatus = "CREATED",
            VideoStatus = "NOT_STARTED",
            LastFacility = "CUSTOMER_CONTENT_CREATED",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.CustomerContents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(new CustomerNormalizeRequestCreatedEvent
        {
            CustomerContentId = id,
            Url = entity.Url,
            CorrelationId = Guid.NewGuid(),
            OutlineProviderKey = entity.OutlineProviderKey,
            VideoProviderKey = entity.VideoProviderKey,
            AudioProviderKey = entity.AudioProviderKey
        }, cancellationToken);

        return id;
    }

    public async Task<Guid> CreateAnalysisContentAsync(
        CreateAnalysisContentRequest request,
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
            Title = request.Title,
            NormalizeStatus = "CREATED",
            VideoStatus = "NOT_STARTED",
            LastFacility = "ANALYSIS_CONTENT_CREATED",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var sort = 1;

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

                return new AnalysisNormalizeItem { CustomerContentId = content.Id, Url = content.Url, Path = content.Path, SortOrder = index + 1 };
            })
            .ToList();

        await _eventBus.PublishAsync(new AnalysisNormalizeRequestCreatedEvent
        {
            AnalysisContentId = analysisId,
            Items = items,
            OutlineProviderKey = analysis.OutlineProviderKey,
            VideoProviderKey = analysis.VideoProviderKey,
            AudioProviderKey = analysis.AudioProviderKey,
            CorrelationId = Guid.NewGuid()
        }, cancellationToken);

        return analysisId;
    }

    public async Task HandleNormalizerResultAsync(NormalizerResultPublishedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.CustomerContentId.HasValue)
        {
            var entity = await _db.CustomerContents.FirstAsync(x => x.Id == @event.CustomerContentId, cancellationToken);
            entity.NormalizeRequestId = @event.NormalizeRequestId;
            entity.NormalizeStatus = "COMPLETED";
            entity.VideoStatus = "APPROVED";
            entity.LastFacility = @event.Facility;
            entity.LastError = null;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (@event.AnalysisContentId.HasValue)
        {
            var entity = await _db.AnalysisContents.FirstAsync(x => x.Id == @event.AnalysisContentId, cancellationToken);
            entity.NormalizeRequestId = @event.NormalizeRequestId;
            entity.NormalizeStatus = "COMPLETED";
            entity.VideoStatus = "APPROVED";
            entity.LastFacility = @event.Facility;
            entity.LastError = null;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _eventBus.PublishAsync(new VideoGenerationApprovedEvent
        {
            CustomerContentId = @event.CustomerContentId,
            AnalysisContentId = @event.AnalysisContentId,
            ContentProcessType = @event.ContentProcessType,
            VideoInputJson = @event.VideoInputJson,
            CorrelationId = @event.CorrelationId,
            VideoProviderKey = @event.VideoProviderKey,
            AudioProviderKey = @event.AudioProviderKey,
        }, cancellationToken);
    }

    public async Task HandleVideoResultAsync(VideoGenerationResultPublishedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.CustomerContentId.HasValue)
        {
            var entity = await _db.CustomerContents.FirstAsync(x => x.Id == @event.CustomerContentId, cancellationToken);
            entity.VideoStatus = "COMPLETED";
            entity.VideoRequestId = @event.VideoRequestId;
            entity.FinalVideoUrl = @event.FinalVideoUrl;
            entity.LastFacility = @event.Facility;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (@event.AnalysisContentId.HasValue)
        {
            var entity = await _db.AnalysisContents.FirstAsync(x => x.Id == @event.AnalysisContentId, cancellationToken);
            entity.VideoStatus = "COMPLETED";
            entity.VideoRequestId = @event.VideoRequestId;
            entity.FinalVideoUrl = @event.FinalVideoUrl;
            entity.LastFacility = @event.Facility;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStepFailedAsync(
        StepFailedEvent @event,
        CancellationToken cancellationToken)
    {
        if (@event.CustomerContentId.HasValue)
        {
            var entity = await _db.CustomerContents
                .FirstAsync(x => x.Id == @event.CustomerContentId.Value, cancellationToken);

            entity.LastFacility = @event.Facility;
            entity.LastError = @event.ErrorMessage;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            if (!@event.Retryable)
            {
                if (IsNormalizeStep(@event.Step))
                    entity.NormalizeStatus = "FAILED";

                if (IsVideoStep(@event.Step))
                    entity.VideoStatus = "FAILED";
            }
        }

        if (@event.AnalysisContentId.HasValue)
        {
            var entity = await _db.AnalysisContents
                .FirstAsync(x => x.Id == @event.AnalysisContentId.Value, cancellationToken);

            entity.LastFacility = @event.Facility;
            entity.LastError = @event.ErrorMessage;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            if (!@event.Retryable)
            {
                if (IsNormalizeStep(@event.Step))
                    entity.NormalizeStatus = "FAILED";

                if (IsVideoStep(@event.Step))
                    entity.VideoStatus = "FAILED";
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsNormalizeStep(string step)
    {
        return step.Contains("SCRAPING", StringComparison.OrdinalIgnoreCase)
               || step.Contains("OUTLINE", StringComparison.OrdinalIgnoreCase)
               || step.Contains("NORMALIZE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVideoStep(string step)
    {
        return step.Contains("AUDIO", StringComparison.OrdinalIgnoreCase)
               || step.Contains("VIDEO", StringComparison.OrdinalIgnoreCase);
    }
}