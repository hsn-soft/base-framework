using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class NormalizedAnalysisClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<NormalizedAnalysis>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.ClientId).SetIsRequired(true);
            map.MapMember(x => x.AnalysisContentId).SetIsRequired(true);
            map.MapMember(x => x.AnalysisDate).SetIsRequired(true);
        });

        if (!BsonClassMap.IsClassMapRegistered(typeof(AnalysisReferenceModel)))
        {
            BsonClassMap.RegisterClassMap<AnalysisReferenceModel>();
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(AnalysisDataModel)))
        {
            BsonClassMap.RegisterClassMap<AnalysisDataModel>();
        }
    }
}