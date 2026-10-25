using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Repositories;
using Abc.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>The users in PostgreSQL: what <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> does for any entity, and the lookup by name that the auth server signs in with.</summary>
public sealed class EfCoreUserRepository(IdentityDbContext db) : EfCoreRepository<IdentityDbContext, User, Guid>(db), IUserRepository
{
    public Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        // nobody is signed in yet, so no tenant to look in: the user is found in every tenant, but a deleted user is not found
        string name = HsnSoft.Base.Text.StringHelper.Normalize(userName);
        return Db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(user => !user.IsDeleted && user.NormalizedUserName == name, cancellationToken);
    }
}
