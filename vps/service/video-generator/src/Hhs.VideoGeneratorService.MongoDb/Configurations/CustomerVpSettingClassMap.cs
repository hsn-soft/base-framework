using Hhs.VideoGeneratorService.Domain.SettingDomain.Consts;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using HsnSoft.Base.MongoDB.Helpers;
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

            map.MapMember(x => x.ScopeKey)
                .SetIsRequired(true)
                .SetMaxLength(CustomerVpSettingConsts.ScopeKeyMaxLength);

            map.MapMember(x => x.AudioProviderKey)
                .SetIsRequired(false)
                .SetMaxLength(CustomerVpSettingConsts.AudioProviderKeyMaxLength);

            map.MapMember(x => x.VideoProviderKey)
                .SetIsRequired(true)
                .SetMaxLength(CustomerVpSettingConsts.VideoProviderKeyMaxLength);
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