using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Repositories;

public interface IAppUserRepository : IReadOnlyGenericRepository<AppUser, Guid>
{
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