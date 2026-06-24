using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class AnalysisContentNormalizedRequestClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(AnalysisContentNormalizedRequest)))
        {
            BsonClassMap.RegisterClassMap<AnalysisContentNormalizedRequest>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
                map.MapIdMember(x => x.Id);
            });
        }
    }
}
