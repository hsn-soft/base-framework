using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.MongoDB;

public interface IMongoDbContextProvider<TMongoDbContext>
    where TMongoDbContext : IBaseMongoDbContext
{
    [Obsolete("Use CreateDbContextAsync")]
    TMongoDbContext GetDbContext();

    Task<TMongoDbContext> GetDbContextAsync(CancellationToken cancellationToken = default);
}