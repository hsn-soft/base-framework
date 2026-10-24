using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Repositories;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Repositories;

public sealed class EfCoreUserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> GetPagedListAsync(int skip, int take, bool? isActive, CancellationToken cancellationToken = default)
        => await Filter(isActive).AsNoTracking().OrderBy(user => user.UserName).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountAsync(bool? isActive, CancellationToken cancellationToken = default) => Filter(isActive).CountAsync(cancellationToken);

    public Task<bool> UserNameExistsAsync(string userName, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        string name = User.Normalize(userName);
        return db.Users.AnyAsync(user => user.NormalizedUserName == name && (exceptId == null || user.Id != exceptId), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await db.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);

    public void Remove(User user) => db.Users.Remove(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);

    private IQueryable<User> Filter(bool? isActive) => isActive is null ? db.Users : db.Users.Where(user => user.IsActive == isActive);
}
