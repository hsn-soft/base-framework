using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Repositories;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Repositories;

public sealed class EfCoreUserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        string name = User.Normalize(userName);
        return db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.NormalizedUserName == name, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetListAsync(UserFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        => await Filter(filter).AsNoTracking().OrderBy(user => user.UserName).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountAsync(UserFilter filter, CancellationToken cancellationToken = default) => Filter(filter).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string text = User.Normalize(searchText);
            query = query.Where(user => user.NormalizedUserName.Contains(text) || user.NormalizedEmail.Contains(text));
        }

        return await query.OrderBy(user => user.UserName).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> UserNameExistsAsync(string userName, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        string name = User.Normalize(userName);
        return db.Users.AnyAsync(user => user.NormalizedUserName == name && (exceptId == null || user.Id != exceptId), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await db.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);

    public void Remove(User user) => db.Users.Remove(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);

    private IQueryable<User> Filter(UserFilter filter)
    {
        IQueryable<User> query = db.Users;
        if (!string.IsNullOrWhiteSpace(filter.UserName))
        {
            string name = User.Normalize(filter.UserName);
            query = query.Where(user => user.NormalizedUserName.Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            string email = User.Normalize(filter.Email);
            query = query.Where(user => user.NormalizedEmail.Contains(email));
        }

        return filter.IsActive is null ? query : query.Where(user => user.IsActive == filter.IsActive);
    }
}
