using HsnSoft.Base.MongoDB;
using HsnSoft.Base.Test.Api.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace HsnSoft.Base.Test.Api.MongoDb.Context;

public class AppMongoDbContext(IConfiguration configuration, IServiceProvider provider = null) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public IMongoCollection<User> Users => GetCollection<User>();
}