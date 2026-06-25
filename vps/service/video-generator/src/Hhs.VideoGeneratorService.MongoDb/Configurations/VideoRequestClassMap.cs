using HsnSoft.Base.MongoDB.Helpers;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Consts;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class VideoRequestClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(VideoRequest)))
        {
            BsonClassMap.RegisterClassMap<VideoRequest>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                // Correlation & Context
                map.MapMember(x => x.CorrelationId)
                    .SetMaxLength(VideoRequestConsts.CorrelationIdMaxLength);

                // Subscription & Scope
                map.MapMember(x => x.ScopeKey)
                    .SetIsRequired(true)
                    .SetMaxLength(VideoRequestConsts.ScopeKeyMaxLength);

                // Status & Configuration
                map.MapMember(x => x.Status)
                    .SetIsRequired(true)
                    .SetMaxLength(VideoRequestConsts.StatusMaxLength);
                map.MapMember(x => x.CurrentStep)
                    .SetIsRequired(true)
                    .SetMaxLength(VideoRequestConsts.CurrentStepMaxLength);

                // Reference Fields
                map.MapMember(x => x.RefContentId).SetIsRequired(true);
                map.MapMember(x => x.RefContentType).SetIsRequired(true);
                map.MapMember(x => x.SourceEventId).SetIsRequired(true);

                // Input Data
                map.MapMember(x => x.MediaInputJson)
                    .SetIsRequired(true)
                    .SetMaxLength(VideoRequestConsts.MediaInputJsonMaxLength);

                // Provider Configuration
                map.MapMember(x => x.AudioProviderKey)
                    .SetMaxLength(VideoRequestConsts.AudioProviderKeyMaxLength);
                map.MapMember(x => x.VideoProviderKey)
                    .SetIsRequired(true)
                    .SetMaxLength(VideoRequestConsts.VideoProviderKeyMaxLength);

                // Video Generation (external provider)
                map.MapMember(x => x.VideoProviderTrackingId)
                    .SetMaxLength(VideoRequestConsts.VideoProviderTrackingIdMaxLength);
                map.MapMember(x => x.VideoProviderUrl)
                    .SetMaxLength(VideoRequestConsts.VideoProviderUrlMaxLength);
                map.MapMember(x => x.VideoLocalPath)
                    .SetMaxLength(VideoRequestConsts.VideoLocalPathMaxLength);

                // Storage & CDN
                map.MapMember(x => x.VideoCdnProviderKey)
                    .SetMaxLength(VideoRequestConsts.VideoCdnProviderKeyMaxLength);
                map.MapMember(x => x.VideoStorageUrl)
                    .SetMaxLength(VideoRequestConsts.VideoStorageUrlMaxLength);
                map.MapMember(x => x.VideoCdnUrl)
                    .SetMaxLength(VideoRequestConsts.VideoCdnUrlMaxLength);

                // Error Handling
                map.MapMember(x => x.LastError)
                    .SetMaxLength(VideoRequestConsts.LastErrorMaxLength);
            });
        }
    }
}
