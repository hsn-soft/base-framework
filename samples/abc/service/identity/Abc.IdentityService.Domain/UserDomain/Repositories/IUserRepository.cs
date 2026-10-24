using Abc.IdentityService.Domain.UserDomain.Entities;

namespace Abc.IdentityService.Domain.UserDomain.Repositories;

/// <summary>Where the users are kept. The domain says what it needs; the persistence project says how.</summary>
public interface IUserRepository
{
    Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The user with this name (compared without regard to case), not tracked; <see langword="null"/> when there is none. What the auth server signs in with.</summary>
    Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>The users that match <paramref name="filter"/>, by user name, from <paramref name="skip"/> on, at most <paramref name="take"/>.</summary>
    Task<IReadOnlyList<User>> GetListAsync(UserFilter filter, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>How many users match <paramref name="filter"/>.</summary>
    Task<int> CountAsync(UserFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The users whose user name or email has <paramref name="searchText"/> in it (all of them when it is empty), by user name, at most <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<User>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default);

    /// <summary>Whether a user has this name (names are compared without regard to case); <paramref name="exceptId"/> leaves one user out.</summary>
    Task<bool> UserNameExistsAsync(string userName, Guid? exceptId = null, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    void Remove(User user);

    /// <summary>Saves what was added, changed or removed.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
