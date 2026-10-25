using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xyz.AuthServer.Domain.UserDomain.Entities;
using Xyz.AuthServer.Domain.UserDomain.Repositories;
using Xyz.AuthServer.Infrastructure.Context;

namespace Xyz.AuthServer.Infrastructure.Repositories;

/// <summary>The users in PostgreSQL: what <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> does for any entity, and the lookup by name that the auth server signs in with.</summary>
public sealed class EfCoreUserRepository(IdentityDbContext db) : EfCoreRepository<IdentityDbContext, User, Guid>(db), IUserRepository
{
    public Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        // nobody is signed in yet, so no tenant to look in: the user is found in every tenant, but a deleted user is not found
        string name = HsnSoft.Base.Text.StringHelper.Normalize(userName);
        return Db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(user => !user.IsDeleted && user.NormalizedUserName == name, cancellationToken);
    }

    public Task<User?> FindByIdAcrossTenantsAsync(Guid id, CancellationToken cancellationToken = default)
        => Db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(user => !user.IsDeleted && user.Id == id, cancellationToken);
}
