using Hhs.TextNormalizerService.Domain.InfraDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class EventInboxMessageClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<EventInboxMessage>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.EventName).SetIsRequired(true);
            map.MapMember(x => x.Payload).SetIsRequired(true);
            map.MapMember(x => x.Status).SetIsRequired(true);
        });
    }
}