using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Sample.Api.Domain.Entities;

namespace HsnSoft.Base.Sample.Api.Domain.Repositories;

public interface IEfCoreUserRepository : IGenericRepository<User, Guid>
{
    Task<List<TResult>> GetListWithMapperAsync<TResult>(
        ListQueryOptions<User> options,
        AutoMapper.IConfigurationProvider configuration,
        CancellationToken cancellationToken = default);
}