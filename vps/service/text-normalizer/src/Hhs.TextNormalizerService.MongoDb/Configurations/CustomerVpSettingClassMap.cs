using Hhs.TextNormalizerService.Domain.SettingDomain.Consts;
using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using HsnSoft.Base.MongoDB.Helpers;
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

            map.MapMember(x => x.ScopeKey)
                .SetIsRequired(true)
                .SetMaxLength(CustomerVpSettingConsts.ScopeKeyMaxLength);

            map.MapMember(x => x.DomainName)
                .SetIsRequired(true)
                .SetMaxLength(CustomerVpSettingConsts.DomainNameMaxLength);

            map.MapMember(x => x.OutlineProviderKey)
                .SetIsRequired(true)
                .SetMaxLength(CustomerVpSettingConsts.OutlineProviderKeyMaxLength);

            // Optional Prompts - MaxLength: CustomerVpSettingConsts.ContentOutlinePromptMaxLength, AnalysisOutlineContentPromptMaxLength, AnalysisOutlineIntroPromptMaxLength, AnalysisOutlineOutroPromptMaxLength
        });
    }
}