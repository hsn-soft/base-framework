using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using Microsoft.Extensions.Configuration;

namespace Hhs.TextNormalizerService.MongoDb.Context;

public sealed class TextNormalizerServiceDbContext(IServiceProvider provider, IConfiguration configuration) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<ContentNormalizedRequest> ContentNormalizedRequests => GetCollection<ContentNormalizedRequest>();
    public ITrackingMongoCollection<AnalysisNormalizedRequest> AnalysisNormalizedRequests => GetCollection<AnalysisNormalizedRequest>();
    public ITrackingMongoCollection<CustomerVpSetting> CustomerVpSettings => GetCollection<CustomerVpSetting>();
}