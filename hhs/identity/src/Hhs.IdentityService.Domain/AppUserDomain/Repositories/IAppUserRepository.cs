using System.Linq.Expressions;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using HsnSoft.Base.Domain.Models;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AppUserDomain.Repositories;

public interface IAppUserRepository
{
    Task<PagedQueryResult<AppUser>> GetPageListAsync(
        PagedQueryOptions<AppUser> options,
        Guid? tenantId = null,
        List<Guid> roleIds = null,
        CancellationToken cancellationToken = default
    );

    Task<List<AppUser>> GetListAsync(
        ListQueryOptions<AppUser> options,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default
    );



    Task<List<AppUser>> GetSearchListAsync(
        Guid? tenantId,
        [CanBeNull] string searchText = null,
        [CanBeNull] string sorting = null,
        int maxResultCount = int.MaxValue,
        CancellationToken cancellationToken = default
    );

    Task<List<string>> GetUserRolesAsync(AppUser currentUser);

    //add default default functions because dbContext is not BaseDbContext
    [ItemCanBeNull]
    Task<AppUser> FindWithIdAsync(Guid id);

    [ItemCanBeNull]
    Task<AppUser> FindAsync(Expression<Func<AppUser, bool>> predicate);

    Task<AppUser> CreateAsync(Guid tenantId, [NotNull] string tenantDomain,
        [NotNull] string userName,
        [NotNull] string email,
        string phone,
        string name,
        string surname,
        string defaultLanguage,
        string avatarSuffixUrl,
        ICollection<string> roles = null,
        string plainPassword = null
    );

    Task<AppUser> CreateAsync(Guid id, Guid tenantId, [NotNull] string tenantDomain,
        [NotNull] string userName,
        [NotNull] string email,
        string phone,
        string name,
        string surname,
        string defaultLanguage,
        string avatarSuffixUrl,
        ICollection<string> roles = null,
        string plainPassword = null
    );

    Task<AppUser> UpdateAsync(Guid id,
        string userName,
        string email,
        string phone,
        string name,
        string surname,
        string defaultLanguage,
        ICollection<string> roles = null);

    Task DeleteAsync(Guid id);
}