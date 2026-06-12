using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using Microsoft.Extensions.Configuration;

namespace Hhs.VideoGeneratorService.MongoDb.Context;

public sealed class VideoGeneratorServiceDbContext(IServiceProvider provider, IConfiguration configuration) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<VideoRequest> VideoRequests => GetCollection<VideoRequest>();
    public ITrackingMongoCollection<CustomerConfiguration> CustomerConfigurations => GetCollection<CustomerConfiguration>();
}