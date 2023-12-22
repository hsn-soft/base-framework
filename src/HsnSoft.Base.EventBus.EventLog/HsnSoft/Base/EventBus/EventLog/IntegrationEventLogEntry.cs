using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.EventLog;

public class IntegrationEventLogEntry
{
    private IntegrationEventLogEntry() { }
    public IntegrationEventLogEntry(MessageEnvelope<IIntegrationEventMessage> @event, Guid transactionId)
    {
        EventId = @event.MessageId;
        CreationTime = @event.MessageTime;
        EventTypeName = @event.GetType().FullName;
        Content = JsonSerializer.Serialize(@event, @event.GetType(), new JsonSerializerOptions
        {
            WriteIndented = true
        });
        State = EventStateEnum.NotPublished;
        TimesSent = 0;
        TransactionId = transactionId.ToString();
    }
    public Guid EventId { get; private set; }
    public string EventTypeName { get; private set; }
    [NotMapped]
    public string EventTypeShortName => EventTypeName.Split('.')?.Last();

    public EventStateEnum State { get; set; }
    public int TimesSent { get; set; }
    public DateTimeOffset CreationTime { get; private set; }
    public string Content { get; private set; }
    public string TransactionId { get; private set; }
}
