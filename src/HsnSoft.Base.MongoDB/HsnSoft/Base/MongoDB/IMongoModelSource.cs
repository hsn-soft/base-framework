namespace HsnSoft.Base.MongoDB;

public interface IMongoModelSource
{
    MongoDbContextModel GetModel(BaseMongoDbContext dbContext);
}