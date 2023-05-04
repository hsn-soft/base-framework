namespace HsnSoft.Base.MongoDB;

public static class BaseMongoDbContextExtensions
{
    public static BaseMongoDbContext ToBaseMongoDbContext(this IBaseMongoDbContext dbContext)
    {
        var baseMongoDbContext = dbContext as BaseMongoDbContext;

        if (baseMongoDbContext == null)
        {
            throw new BaseException($"The type {dbContext.GetType().AssemblyQualifiedName} should be convertable to {typeof(BaseMongoDbContext).AssemblyQualifiedName}!");
        }

        return baseMongoDbContext;
    }
}