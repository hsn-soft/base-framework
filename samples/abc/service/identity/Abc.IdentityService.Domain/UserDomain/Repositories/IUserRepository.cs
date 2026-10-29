using Abc.IdentityService.Domain.UserDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Abc.IdentityService.Domain.UserDomain.Repositories;

/// <summary>Where the users are kept: what every repository does (<see cref="IRepository{TEntity, TKey}"/>) and the one thing the auth server needs besides.</summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// The user with this name (compared without regard to case), not tracked, in whichever tenant it is: nobody is signed in yet, so there is no tenant to look in. A deleted user is not found.
    /// </summary>
    Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
}
