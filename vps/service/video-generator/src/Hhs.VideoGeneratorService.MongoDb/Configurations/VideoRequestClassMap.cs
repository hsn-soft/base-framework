using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class VideoRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<VideoRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);

            // Correlation & Context - MaxLength: VideoRequestConsts.CorrelationIdMaxLength
            // Subscription & Scope - MaxLength: VideoRequestConsts.ScopeKeyMaxLength
            map.MapMember(x => x.ScopeKey).SetIsRequired(true);

            // Status & Configuration - MaxLength: VideoRequestConsts.StatusMaxLength, VideoRequestConsts.CurrentStepMaxLength
            map.MapMember(x => x.Status).SetIsRequired(true);
            map.MapMember(x => x.CurrentStep).SetIsRequired(true);

            // Reference Fields
            map.MapMember(x => x.RefContentId).SetIsRequired(true);
            map.MapMember(x => x.RefContentType).SetIsRequired(true);
            map.MapMember(x => x.SourceEventId).SetIsRequired(true);

            // Input Data - MaxLength: VideoRequestConsts.MediaInputJsonMaxLength
            map.MapMember(x => x.MediaInputJson).SetIsRequired(true);

            // Provider Configuration
            // MaxLength: VideoRequestConsts.AudioProviderKeyMaxLength
            // MaxLength: VideoRequestConsts.VideoProviderKeyMaxLength
            map.MapMember(x => x.VideoProviderKey).SetIsRequired(true);

            // Video Generation (external provider)
            // MaxLength: VideoRequestConsts.VideoProviderTrackingIdMaxLength
            // MaxLength: VideoRequestConsts.VideoProviderUrlMaxLength
            // MaxLength: VideoRequestConsts.VideoLocalPathMaxLength

            // Storage & CDN
            // MaxLength: VideoRequestConsts.VideoCdnProviderKeyMaxLength
            // MaxLength: VideoRequestConsts.VideoStorageUrlMaxLength
            // MaxLength: VideoRequestConsts.VideoCdnUrlMaxLength

            // Error Handling - MaxLength: VideoRequestConsts.LastErrorMaxLength
        });
    }
}
