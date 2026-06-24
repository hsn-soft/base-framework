using Hhs.FeedRService.Domain.ReportingDomain.Consts;
using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using Hhs.FeedRService.Domain.ReportingDomain.Models;
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
            // Explicit field mappings with MaxLength constraints
            map.MapMember(x => x.AdUnitIdTopLevel);
            // MaxLength: GoogleAdManagerReportRowConsts.AdUnitIdTopLevelMaxLength

            map.MapMember(x => x.AdUnitId);
            // MaxLength: GoogleAdManagerReportRowConsts.AdUnitIdMaxLength

            map.MapMember(x => x.DemandChannel);
            // MaxLength: GoogleAdManagerReportRowConsts.DemandChannelMaxLength

            map.MapMember(x => x.DemandSubchannelName);
            // MaxLength: GoogleAdManagerReportRowConsts.DemandSubchannelNameMaxLength

            map.MapMember(x => x.OrderId);
            // MaxLength: GoogleAdManagerReportRowConsts.OrderIdMaxLength

            map.MapMember(x => x.OrderName);
            // MaxLength: GoogleAdManagerReportRowConsts.OrderNameMaxLength

            map.MapMember(x => x.Date);
            // MaxLength: GoogleAdManagerReportRowConsts.DateMaxLength
        });

        BsonClassMap.RegisterClassMap<RawGoogleAdManagerResponse>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.RequestId).SetIsRequired(true);
            // MaxLength: RawGoogleAdManagerResponseConsts.RequestIdMaxLength

            map.MapMember(x => x.JobName).SetIsRequired(true);
            // MaxLength: RawGoogleAdManagerResponseConsts.JobNameMaxLength

            map.MapMember(x => x.ClientName);
            // MaxLength: RawGoogleAdManagerResponseConsts.ClientNameMaxLength

            map.MapMember(x => x.Network);
            // MaxLength: RawGoogleAdManagerResponseConsts.NetworkMaxLength

            map.MapMember(x => x.AdUnitIdTopLevel);
            // MaxLength: RawGoogleAdManagerResponseConsts.AdUnitIdTopLevelMaxLength

            map.MapMember(x => x.AdUnitId);
            // MaxLength: RawGoogleAdManagerResponseConsts.AdUnitIdMaxLength

            map.MapMember(x => x.RawResponse);
            // MaxLength: RawGoogleAdManagerResponseConsts.RawResponseMaxLength

            map.MapMember(x => x.ProcessingError);
            // MaxLength: RawGoogleAdManagerResponseConsts.ProcessingErrorMaxLength
        });
    }
}
