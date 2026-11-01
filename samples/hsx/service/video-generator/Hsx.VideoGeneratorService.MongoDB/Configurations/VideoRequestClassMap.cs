using HsnSoft.Base.MongoDB;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hsx.VideoGeneratorService.MongoDB.Configurations;

/// <summary>How a <see cref="VideoRequest"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class VideoRequestClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(VideoRequest)))
        {
            return;
        }

        // the id and the audit fields are the base classes' (their maps first); this map has what is the entity's own
        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<VideoRequest>(map =>
        {
            map.MapProperty(entity => entity.ScopeKey);
            map.MapProperty(entity => entity.RefContentId);
            map.MapProperty(entity => entity.RefContentType);
            map.MapProperty(entity => entity.Status);
            map.MapProperty(entity => entity.CurrentMilestone);
            map.MapProperty(entity => entity.MediaInputJson);
            map.MapProperty(entity => entity.AudioProviderKey);
            map.MapProperty(entity => entity.VideoProviderKey);
            map.MapProperty(entity => entity.VideoProviderTrackingId);
            map.MapProperty(entity => entity.VideoProviderUrl);
            map.MapProperty(entity => entity.VideoStorageUrl);
            map.MapProperty(entity => entity.VideoCdnUrl);
            map.MapProperty(entity => entity.RetryCount);
            map.MapProperty(entity => entity.LastError);
        });
    }
}
