using Hhs.EventManagerService.Domain.EventDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.EventManagerService.MongoDb.Configurations;

public static class FailedIntegrationEventClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<FailedIntegrationEvent>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.FailedReason).SetIsRequired(true);
        });
    }
}