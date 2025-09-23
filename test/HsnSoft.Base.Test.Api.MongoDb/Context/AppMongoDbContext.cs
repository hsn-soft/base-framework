using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using HsnSoft.Base.Test.Api.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HsnSoft.Base.Test.Api.MongoDb.Context;

public class AppMongoDbContext(IConfiguration configuration, IServiceProvider provider = null) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<User> Users => GetCollection<User>();
}