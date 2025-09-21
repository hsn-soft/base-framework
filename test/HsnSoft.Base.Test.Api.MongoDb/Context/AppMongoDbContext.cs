using HsnSoft.Base.MongoDB;
using HsnSoft.Base.Test.Api.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace HsnSoft.Base.Test.Api.MongoDb.Context;

public class AppMongoDbContext(IConfiguration configuration) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName))
{
    public IMongoCollection<User> Users => GetCollection<User>();
}