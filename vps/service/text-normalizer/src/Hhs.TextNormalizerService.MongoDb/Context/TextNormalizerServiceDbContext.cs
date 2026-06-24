using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using Microsoft.Extensions.Configuration;

namespace Hhs.TextNormalizerService.MongoDb.Context;

public sealed class TextNormalizerServiceDbContext(IServiceProvider provider, IConfiguration configuration) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<CustomerVpSetting> CustomerVpSettings => GetCollection<CustomerVpSetting>();

    public ITrackingMongoCollection<CustomerContentNormalizedRequest> CustomerContentNormalizedRequests => GetCollection<CustomerContentNormalizedRequest>();
    public ITrackingMongoCollection<AnalysisContentNormalizedRequest> AnalysisContentNormalizedRequests => GetCollection<AnalysisContentNormalizedRequest>();
    public ITrackingMongoCollection<EventInboxMessage> NormalizerInboxMessages => GetCollection<EventInboxMessage>();
}