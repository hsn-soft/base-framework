using System;
using System.Text.Json.Serialization;

namespace HsnSoft.Base.Domain.Entities.Events;

public record IntegrationEvent
{
    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public DateTime CreationDate { get; private set; }

    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    // [Newtonsoft.Json.JsonConstructor]
    public IntegrationEvent(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }
}