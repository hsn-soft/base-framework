using HsnSoft.Base.MongoDB.Helpers;
using Hhs.FeedRService.Domain.ConfigurationDomain.Consts;
using Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;
using MongoDB.Bson.Serialization;

namespace Hhs.FeedRService.MongoDb.Configurations;

public static class CustomerConfigurationClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<CustomerConfiguration>(map =>
        {
            map.AutoMap();
            map.MapIdMember(x => x.Id);
            map.SetIgnoreExtraElements(true);

            map.MapMember(x => x.ClientName)
                .SetIsRequired(true)
                .SetMaxLength(CustomerConfigurationConsts.ClientNameMaxLength);

            map.MapMember(x => x.AdUnitIdTopLevel)
                .SetIsRequired(true)
                .SetMaxLength(CustomerConfigurationConsts.AdUnitIdTopLevelMaxLength);

            map.MapMember(x => x.Network)
                .SetMaxLength(CustomerConfigurationConsts.NetworkMaxLength);

            map.MapMember(x => x.AdUnitName)
                .SetMaxLength(CustomerConfigurationConsts.AdUnitNameMaxLength);
        });
    }
}