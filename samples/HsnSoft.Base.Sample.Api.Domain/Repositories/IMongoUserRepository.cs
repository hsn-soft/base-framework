using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Sample.Api.Domain.Entities;

namespace HsnSoft.Base.Sample.Api.Domain.Repositories;

public interface IMongoUserRepository : IGenericRepository<User, Guid>;