using Hhs.FeedRService.Domain.ConfigurationDomain.Entities;
using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using Microsoft.Extensions.Configuration;

namespace Hhs.FeedRService.MongoDb.Context;

public sealed class FeedRServiceDbContext(IServiceProvider provider, IConfiguration configuration)
    : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<CustomerConfiguration> CustomerConfigurations => GetCollection<CustomerConfiguration>();
    public ITrackingMongoCollection<NetworkConfiguration> NetworkConfigurations => GetCollection<NetworkConfiguration>();
    public ITrackingMongoCollection<RawGoogleAdManagerResponse> RawGoogleAdManagerResponses => GetCollection<RawGoogleAdManagerResponse>();
}