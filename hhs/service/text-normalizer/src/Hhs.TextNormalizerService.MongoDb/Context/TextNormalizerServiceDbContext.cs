using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using Microsoft.Extensions.Configuration;

namespace Hhs.TextNormalizerService.MongoDb.Context;

public sealed class TextNormalizerServiceDbContext(IServiceProvider provider, IConfiguration configuration) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<NormalizedRequest> NormalizedRequests => GetCollection<NormalizedRequest>();
    public ITrackingMongoCollection<NormalizedAnalysis> NormalizedAnalysis => GetCollection<NormalizedAnalysis>();
    public ITrackingMongoCollection<CustomerConfiguration> CustomerConfigurations => GetCollection<CustomerConfiguration>();
}