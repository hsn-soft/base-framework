using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class CustomerConfigurationClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<CustomerConfiguration>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.ClientName).SetIsRequired(true);
        });

        if (!BsonClassMap.IsClassMapRegistered(typeof(ClientDidAiSettings)))
        {
            BsonClassMap.RegisterClassMap<ClientDidAiSettings>();
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ClientHeyGenSettings)))
        {
            BsonClassMap.RegisterClassMap<ClientHeyGenSettings>();
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ClientColossyanAiSettings)))
        {
            BsonClassMap.RegisterClassMap<ClientColossyanAiSettings>();
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ClientYepicAiSettings)))
        {
            BsonClassMap.RegisterClassMap<ClientYepicAiSettings>();
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ClientElevenLabsSettings)))
        {
            BsonClassMap.RegisterClassMap<ClientElevenLabsSettings>();
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ClientCreatomateSettings)))
        {
            BsonClassMap.RegisterClassMap<ClientCreatomateSettings>();
        }
    }
}