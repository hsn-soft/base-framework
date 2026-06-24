using Hhs.FeedRService.Domain.ConfigurationDomain.Entities;
using Hhs.FeedRService.Domain.ReportingDomain.Consts;
using MongoDB.Bson.Serialization;

namespace Hhs.FeedRService.MongoDb.Configurations;

public static class CustomerConfigurationClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<CustomerConfiguration>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.ClientName).SetIsRequired(true);
            // MaxLength: CustomerConfigurationConsts.ClientNameMaxLength

            map.MapMember(x => x.AdUnitIdTopLevel).SetIsRequired(true);
            // MaxLength: CustomerConfigurationConsts.AdUnitIdTopLevelMaxLength

            map.MapMember(x => x.Network);
            // MaxLength: CustomerConfigurationConsts.NetworkMaxLength (optional)

            map.MapMember(x => x.AdUnitName);
            // MaxLength: CustomerConfigurationConsts.AdUnitNameMaxLength (optional)
        });
    }
}