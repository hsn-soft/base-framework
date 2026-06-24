using Hhs.FeedRService.Domain.ConfigurationDomain.Entities;
using Hhs.FeedRService.Domain.ConfigurationDomain.Models;
using MongoDB.Bson.Serialization;

namespace Hhs.FeedRService.MongoDb.Configurations;

public static class NetworkConfigurationClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<TopLevelGroupConfig>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
        });

        BsonClassMap.RegisterClassMap<NetworkConfiguration>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.NetworkCode).SetIsRequired(true);
        });
    }
}
