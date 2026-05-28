using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Repositories;

public interface IAppUserRepository : IReadOnlyGenericRepository<AppUser, Guid>
{
    Task<AppUser> CreateAsync(Guid tenantId,
        [NotNull] string userName,
        [NotNull] string email,
        [NotNull] string passwordHash,
        bool isStatic = false,
        [CanBeNull] string displayName = null,
        [CanBeNull] string avatarSuffixUrl = null,
        [CanBeNull] string phoneNumber = null,
        [CanBeNull] string languageCode = null
    );

    Task<AppUser> CreateAsync(Guid id, Guid tenantId,
        [NotNull] string userName,
        [NotNull] string email,
        [NotNull] string passwordHash,
        bool isStatic = false,
        [CanBeNull] string displayName = null,
        [CanBeNull] string avatarSuffixUrl = null,
        [CanBeNull] string phoneNumber = null,
        [CanBeNull] string languageCode = null
    );

    Task<AppUser> UpdateAsync(Guid id,
        [NotNull] string userName,
        [NotNull] string email,
        [CanBeNull] string displayName = null,
        [CanBeNull] string avatarSuffixUrl = null,
        [CanBeNull] string phoneNumber = null,
        [CanBeNull] string languageCode = null);

    Task SetLoginFailureStatesAsync(Guid tenantId, Guid appUserId, int failedLoginCount);
    Task SetLoginSuccessStatesAsync(Guid appUserId);

    Task DeleteAsync(Guid id);
}