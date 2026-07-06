using Hhs.Shared.Helper.EventInbox;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

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