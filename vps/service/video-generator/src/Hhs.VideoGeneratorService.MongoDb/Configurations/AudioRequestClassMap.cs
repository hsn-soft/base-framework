using HsnSoft.Base.MongoDB.Helpers;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Consts;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class AudioRequestClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(AudioRequest)))
        {
            BsonClassMap.RegisterClassMap<AudioRequest>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                // Correlation & Context
                map.MapMember(x => x.CorrelationId)
                    .SetMaxLength(AudioRequestConsts.CorrelationIdMaxLength);

                // Subscription & Scope
                map.MapMember(x => x.ScopeKey)
                    .SetIsRequired(true)
                    .SetMaxLength(AudioRequestConsts.ScopeKeyMaxLength);

                // Status & Configuration
                map.MapMember(x => x.Status)
                    .SetIsRequired(true)
                    .SetMaxLength(AudioRequestConsts.StatusMaxLength);
                map.MapMember(x => x.CurrentMilestone)
                    .SetIsRequired(true)
                    .SetMaxLength(AudioRequestConsts.CurrentMilestoneMaxLength);

                // Reference Fields
                map.MapMember(x => x.RefContentId).SetIsRequired(true);
                map.MapMember(x => x.RefContentType).SetIsRequired(true);
                map.MapMember(x => x.CustomerContentIdForItem).SetIsRequired(true);
                map.MapMember(x => x.SourceEventId).SetIsRequired(true);
                map.MapMember(x => x.VideoRequestId).SetIsRequired(true);

                // Input Data
                map.MapMember(x => x.InputText)
                    .SetIsRequired(true)
                    .SetMaxLength(AudioRequestConsts.InputTextMaxLength);

                // Provider Configuration
                map.MapMember(x => x.AudioProviderKey)
                    .SetIsRequired(true)
                    .SetMaxLength(AudioRequestConsts.AudioProviderKeyMaxLength);

                // Audio Generation (external provider)
                map.MapMember(x => x.AudioProviderTrackingId)
                    .SetMaxLength(AudioRequestConsts.AudioProviderTrackingIdMaxLength);
                map.MapMember(x => x.AudioProviderUrl)
                    .SetMaxLength(AudioRequestConsts.AudioProviderUrlMaxLength);
                map.MapMember(x => x.AudioLocalPath)
                    .SetMaxLength(AudioRequestConsts.AudioLocalPathMaxLength);

                // Storage & CDN
                map.MapMember(x => x.AudioStorageUrl)
                    .SetMaxLength(AudioRequestConsts.AudioStorageUrlMaxLength);
                map.MapMember(x => x.AudioCdnUrl)
                    .SetMaxLength(AudioRequestConsts.AudioCdnUrlMaxLength);
                map.MapMember(x => x.AudioCdnProviderKey)
                    .SetMaxLength(AudioRequestConsts.AudioCdnProviderKeyMaxLength);

                // Error Handling
                map.MapMember(x => x.LastError)
                    .SetMaxLength(AudioRequestConsts.LastErrorMaxLength);
            });
        }
    }
}
