using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class AudioRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<AudioRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);

            // Correlation & Context - MaxLength: AudioRequestConsts.CorrelationIdMaxLength
            // Subscription & Scope - MaxLength: AudioRequestConsts.ScopeKeyMaxLength
            map.MapMember(x => x.ScopeKey).SetIsRequired(true);

            // Status & Configuration - MaxLength: AudioRequestConsts.StatusMaxLength, AudioRequestConsts.CurrentStepMaxLength
            map.MapMember(x => x.Status).SetIsRequired(true);
            map.MapMember(x => x.CurrentStep).SetIsRequired(true);

            // Reference Fields
            map.MapMember(x => x.RefContentId).SetIsRequired(true);
            map.MapMember(x => x.RefContentType).SetIsRequired(true);
            map.MapMember(x => x.SourceEventId).SetIsRequired(true);
            map.MapMember(x => x.VideoRequestId).SetIsRequired(true);

            // Input Data - MaxLength: AudioRequestConsts.InputTextMaxLength
            map.MapMember(x => x.InputText).SetIsRequired(true);

            // Provider Configuration - MaxLength: AudioRequestConsts.AudioProviderKeyMaxLength
            map.MapMember(x => x.AudioProviderKey).SetIsRequired(true);

            // Audio Generation (external provider)
            // MaxLength: AudioRequestConsts.AudioProviderTrackingIdMaxLength
            // MaxLength: AudioRequestConsts.AudioProviderUrlMaxLength
            // MaxLength: AudioRequestConsts.AudioLocalPathMaxLength

            // Storage & CDN
            // MaxLength: AudioRequestConsts.AudioStorageUrlMaxLength
            // MaxLength: AudioRequestConsts.AudioCdnUrlMaxLength
            // MaxLength: AudioRequestConsts.AudioCdnProviderKeyMaxLength

            // Error Handling - MaxLength: AudioRequestConsts.LastErrorMaxLength
        });
    }
}
