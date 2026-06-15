using System.Text.Json;
using Hhs.Shared.Events;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Infrastructure;

public sealed class VideoGeneratorInboxStore
{
    private readonly VideoMongoContext _context;

    public VideoGeneratorInboxStore(VideoMongoContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _context.InboxMessages
            .Find(x => x.EventId == eventId)
            .AnyAsync(cancellationToken);
    }

    public async Task SaveAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        await _context.InboxMessages.InsertOneAsync(new VideoGeneratorInboxMessage
        {
            EventId = @event.EventId,
            EventName = @event.EventName,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            ProcessedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
    }
}