using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Test.Api.Domain.Entities;

namespace HsnSoft.Base.Test.Api.Domain.Repositories;

public interface IMongoUserRepository : IGenericRepository<User, Guid>
{

}