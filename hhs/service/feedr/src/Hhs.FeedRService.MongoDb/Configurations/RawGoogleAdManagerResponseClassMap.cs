using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.FeedRService.MongoDb.Configurations;

public static class RawGoogleAdManagerResponseClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<GoogleAdManagerReportRow>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
        });

        BsonClassMap.RegisterClassMap<RawGoogleAdManagerResponse>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.RequestId).SetIsRequired(true);
            map.MapMember(x => x.JobName).SetIsRequired(true);
        });
    }
}
