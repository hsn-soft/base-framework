using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

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

            // Optional Prompts - MaxLength: CustomerVpSettingConsts.ContentOutlinePromptMaxLength, AnalysisOutlineContentPromptMaxLength, AnalysisOutlineIntroPromptMaxLength, AnalysisOutlineOutroPromptMaxLength
        });
    }
}