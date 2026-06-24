using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using Microsoft.Extensions.Configuration;

namespace Hhs.VideoGeneratorService.MongoDb.Context;

public sealed class VideoGeneratorServiceDbContext(IServiceProvider provider, IConfiguration configuration) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<CustomerVpSetting> CustomerVpSettings => GetCollection<CustomerVpSetting>();

    public ITrackingMongoCollection<VideoRequest> VideoRequests => GetCollection<VideoRequest>();
    public ITrackingMongoCollection<AudioRequest> AudioRequests => GetCollection<AudioRequest>();
    public ITrackingMongoCollection<VideoGeneratorInboxMessage> VideoGeneratorInboxMessages => GetCollection<VideoGeneratorInboxMessage>();
}