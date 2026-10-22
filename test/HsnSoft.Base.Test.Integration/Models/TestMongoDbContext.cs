using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace HsnSoft.Base.Test.Integration.Models;

public class TestMongoDbContext(string connectionString) : BaseMongoDbContext(connectionString)
{
    public IMongoCollection<TestEntity> TestEntities => GetCollection<TestEntity>();

}

