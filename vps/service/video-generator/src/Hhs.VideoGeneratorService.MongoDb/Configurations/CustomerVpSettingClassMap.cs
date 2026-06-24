using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class CustomerVpSettingClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<CustomerVpSetting>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);

            // Subscription & Scope - MaxLength: CustomerVpSettingConsts.ScopeKeyMaxLength, CustomerVpSettingConsts.DomainNameMaxLength
            map.MapMember(x => x.ScopeKey).SetIsRequired(true);
            map.MapMember(x => x.DomainName).SetIsRequired(true);

            // Optional Customer Zone Settings - MaxLength: CustomerVpSettingConsts.CustomerZoneNameMaxLength, CustomerBucketKeyMaxLength, CustomerBucketSecretMaxLength
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