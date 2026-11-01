using HsnSoft.Base.MongoDB;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hsx.TextNormalizerService.MongoDB.Configurations;

/// <summary>How a <see cref="CustomerContentNormalizedRequest"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class CustomerContentNormalizedRequestClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(CustomerContentNormalizedRequest)))
        {
            return;
        }

        // the id and the audit fields are the base classes' (their maps first); this map has what is the entity's own
        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<CustomerContentNormalizedRequest>(map =>
        {
            map.MapProperty(entity => entity.ScopeKey);
            map.MapProperty(entity => entity.CustomerContentId);
            map.MapProperty(entity => entity.DomainName);
            map.MapProperty(entity => entity.ContentKey);
            map.MapProperty(entity => entity.NormalizedContentKey);
            map.MapProperty(entity => entity.CorrelationId);
            map.MapProperty(entity => entity.Status);
            map.MapProperty(entity => entity.CurrentMilestone);
            map.MapProperty(entity => entity.ScrapingStatus);
            map.MapProperty(entity => entity.OutlineStatus);
            map.MapProperty(entity => entity.RetryCount);
            map.MapProperty(entity => entity.LastError);
        });
    }
}
