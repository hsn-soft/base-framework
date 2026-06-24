using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class AnalysisContentNormalizedRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<AnalysisContentNormalizedRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapIdMember(x => x.Id);
        });

        if (!BsonClassMap.IsClassMapRegistered(typeof(AnalysisNormalizedItem)))
        {
            BsonClassMap.RegisterClassMap<AnalysisNormalizedItem>();
        }
    }
}