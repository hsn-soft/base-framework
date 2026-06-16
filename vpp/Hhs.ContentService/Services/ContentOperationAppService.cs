using System.Text.Json;
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

    public async Task<Guid> CreateCustomerContentAsync(string url, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        var entity = new CustomerContent
        {
            Id = id,
            Url = url,
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
            Url = url,
            CorrelationId = Guid.NewGuid(),
            OutlineProviderKey = entity.OutlineProviderKey,
            VideoProviderKey = entity.VideoProviderKey,
            AudioProviderKey = entity.AudioProviderKey,
        }, cancellationToken);

        return id;
    }

    public async Task<Guid> CreateAnalysisContentAsync(string title, List<Guid> customerContentIds, CancellationToken cancellationToken)
    {
        var analysisId = Guid.NewGuid();

        var contents = await _db.CustomerContents
            .Where(x => customerContentIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var analysis = new AnalysisContent
        {
            Id = analysisId,
            Title = title,
            NormalizeStatus = "CREATED",
            VideoStatus = "NOT_STARTED",
            LastFacility = "ANALYSIS_CONTENT_CREATED",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var sort = 1;

        foreach (var contentId in customerContentIds)
        {
            analysis.Items.Add(new AnalysisContentItem
            {
                Id = Guid.NewGuid(),
                AnalysisContentId = analysisId,
                CustomerContentId = contentId,
                SortOrder = sort++
            });
        }

        _db.AnalysisContents.Add(analysis);
        await _db.SaveChangesAsync(cancellationToken);

        var items = contents
            .Select((x, index) => new AnalysisNormalizeItem
            {
                CustomerContentId = x.Id,
                Url = x.Url,
                Path = x.Path,
                SortOrder = index + 1
            })
            .ToList();

        await _eventBus.PublishAsync(new AnalysisNormalizeRequestCreatedEvent
        {
            AnalysisContentId = analysisId,
            Items = items,
            CorrelationId = Guid.NewGuid()
        }, cancellationToken);

        return analysisId;
    }

    public async Task HandleNormalizerResultAsync(NormalizerResultPublishedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.CustomerContentId.HasValue)
        {
            var entity = await _db.CustomerContents.FirstAsync(x => x.Id == @event.CustomerContentId, cancellationToken);
            entity.NormalizeStatus = "COMPLETED";
            entity.LastFacility = @event.Facility;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (@event.AnalysisContentId.HasValue)
        {
            var entity = await _db.AnalysisContents.FirstAsync(x => x.Id == @event.AnalysisContentId, cancellationToken);
            entity.NormalizeStatus = "COMPLETED";
            entity.LastFacility = @event.Facility;
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
            VideoProviderKey =  @event.VideoProviderKey,
            AudioProviderKey =  @event.AudioProviderKey,
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
}