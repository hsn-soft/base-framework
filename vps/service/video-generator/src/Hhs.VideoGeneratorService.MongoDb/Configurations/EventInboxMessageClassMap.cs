using Hhs.VideoGeneratorService.Domain.InfraDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class EventInboxMessageClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(EventInboxMessage)))
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
}
