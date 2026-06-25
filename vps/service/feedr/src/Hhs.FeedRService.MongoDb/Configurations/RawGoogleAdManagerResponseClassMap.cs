using HsnSoft.Base.MongoDB.Helpers;
using Hhs.FeedRService.Domain.ReportingDomain.Consts;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.MongoDB;
using Hhs.FeedRService.Domain.ReportingDomain.Models;
using MongoDB.Bson.Serialization;

namespace Hhs.FeedRService.MongoDb.Configurations;

public static class RawGoogleAdManagerResponseClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(GoogleAdManagerReportRow)))
        {
            BsonClassMap.RegisterClassMap<GoogleAdManagerReportRow>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                map.MapMember(x => x.AdUnitIdTopLevel);

                map.MapMember(x => x.AdUnitId);

                map.MapMember(x => x.DemandChannel);

                map.MapMember(x => x.DemandSubchannelName);

                map.MapMember(x => x.OrderId);

                map.MapMember(x => x.OrderName);

                map.MapMember(x => x.Date);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(RawGoogleAdManagerResponse)))
        {
            BsonClassMap.RegisterClassMap<RawGoogleAdManagerResponse>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                map.MapMember(x => x.RequestId)
                    .SetIsRequired(true)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.RequestIdMaxLength);

                map.MapMember(x => x.JobName)
                    .SetIsRequired(true)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.JobNameMaxLength);

                map.MapMember(x => x.ClientName)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.ClientNameMaxLength);

                map.MapMember(x => x.Network)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.NetworkMaxLength);

                map.MapMember(x => x.AdUnitIdTopLevel)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.AdUnitIdTopLevelMaxLength);

                map.MapMember(x => x.AdUnitId)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.AdUnitIdMaxLength);

                map.MapMember(x => x.RawResponse)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.RawResponseMaxLength);

                map.MapMember(x => x.ProcessingError)
                    .SetMaxLength(RawGoogleAdManagerResponseConsts.ProcessingErrorMaxLength);
            });
        }
    }
}
