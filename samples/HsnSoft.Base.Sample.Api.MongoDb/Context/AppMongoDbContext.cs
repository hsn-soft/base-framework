using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using HsnSoft.Base.Sample.Api.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HsnSoft.Base.Sample.Api.MongoDb.Context;

public class AppMongoDbContext(IConfiguration configuration, IServiceProvider provider = null) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<User> Users => GetCollection<User>();
}