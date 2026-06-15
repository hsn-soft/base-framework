using System.Text.Json;
using Hhs.ContentService.Data;
using Hhs.ContentService.Entities;
using Hhs.ContentService.Services;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;

namespace Hhs.ContentService.Handlers;

public abstract class ContentEventHandlerBase<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    private readonly ContentDbContext _db;

    protected ContentEventHandlerBase(ContentDbContext db)
    {
        _db = db;
    }

    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken)
    {
        if (await _db.InboxMessages.FindAsync([@event.EventId], cancellationToken) is not null)
            return;

        _db.InboxMessages.Add(new InboxMessage
        {
            EventId = @event.EventId,
            EventName = @event.EventName,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            ProcessedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        await ExecuteAsync(@event, cancellationToken);
    }

    protected abstract Task ExecuteAsync(TEvent @event, CancellationToken cancellationToken);
}

public sealed class NormalizerResultPublishedEventHandler
    : ContentEventHandlerBase<NormalizerResultPublishedEvent>
{
    private readonly ContentOperationAppService _appService;

    public NormalizerResultPublishedEventHandler(ContentDbContext db, ContentOperationAppService appService) : base(db)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(NormalizerResultPublishedEvent @event, CancellationToken cancellationToken)
        => _appService.HandleNormalizerResultAsync(@event, cancellationToken);
}

public sealed class VideoGenerationResultPublishedEventHandler
    : ContentEventHandlerBase<VideoGenerationResultPublishedEvent>
{
    private readonly ContentOperationAppService _appService;

    public VideoGenerationResultPublishedEventHandler(ContentDbContext db, ContentOperationAppService appService) : base(db)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoGenerationResultPublishedEvent @event, CancellationToken cancellationToken)
        => _appService.HandleVideoResultAsync(@event, cancellationToken);
}