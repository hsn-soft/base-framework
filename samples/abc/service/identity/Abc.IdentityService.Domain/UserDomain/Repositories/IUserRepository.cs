using Abc.IdentityService.Domain.UserDomain.Entities;

namespace Abc.IdentityService.Domain.UserDomain.Repositories;

/// <summary>Where the users are kept. The domain says what it needs; the persistence project says how.</summary>
public interface IUserRepository
{
    Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the users by name, only the active (or only the inactive) ones when <paramref name="isActive"/> is given.</summary>
    Task<IReadOnlyList<User>> GetPagedListAsync(int skip, int take, bool? isActive, CancellationToken cancellationToken = default);

    Task<int> CountAsync(bool? isActive, CancellationToken cancellationToken = default);

    /// <summary>Whether a user has this name (names are compared without regard to case); <paramref name="exceptId"/> leaves one user out.</summary>
    Task<bool> UserNameExistsAsync(string userName, Guid? exceptId = null, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    void Remove(User user);

    /// <summary>Saves what was added, changed or removed.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
