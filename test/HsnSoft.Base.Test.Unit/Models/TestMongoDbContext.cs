using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace HsnSoft.Base.Test.Unit.Models;

public class TestMongoDbContext(string connectionString) : BaseMongoDbContext(connectionString)
{
    public IMongoCollection<TestEntity> TestEntities => Collection<TestEntity>();

}

