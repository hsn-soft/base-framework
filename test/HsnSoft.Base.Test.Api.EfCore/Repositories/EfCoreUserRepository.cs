using System.Linq.Dynamic.Core;
using AutoMapper.QueryableExtensions;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.Domain.Repositories;
using HsnSoft.Base.Test.Api.EfCore.Context;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Test.Api.EfCore.Repositories;

// public interface IMyGenericRepository<TEntity> : IEfCoreGenericRepository<AppDbContext, TEntity, Guid> where TEntity : class, IEntity<Guid>;

// public sealed class MyGenericRepository<TEntity>(AppDbContext context) : EfCoreGenericRepository<AppDbContext, TEntity, Guid>(context), IMyGenericRepository<TEntity> where TEntity : class, IEntity<Guid>;

public sealed class EfCoreUserRepository(AppEfCoreDbContext context, IServiceProvider provider) : EfCoreGenericRepository<User, Guid>(provider, context), IEfCoreUserRepository
{
    public async Task<List<TResult>> GetListWithMapperAsync<TResult>(
        ListQueryOptions<User> options,
        AutoMapper.IConfigurationProvider configuration,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = GetDbSet();

        query = query.AsNoTracking();
        if (options.Filter != null) query = query.Where(options.Filter);

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.MaxResultCount.HasValue) query = query.Take((int)options.MaxResultCount.Value);

        return await query.ProjectTo<TResult>(configuration).ToListAsync(cancellationToken: cancellationToken);
    }
}