using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Repositories;

public interface IPermissionGrantRepository : IReadOnlyGenericRepository<PermissionGrant, Guid>
{
    Task<List<PermissionGrant>> GetSessionPermissionsAsync(
        [CanBeNull] string clientKey = null,
        [CanBeNull] string[] roleKeys = null,
        [CanBeNull] string userKey = null,
        CancellationToken cancellationToken = default
    );

    Task<PermissionGrant> CreateAsync(
        [NotNull] string name,
        [NotNull] string providerName,
        [NotNull] string providerKey);

    Task<PermissionGrant> CreateAsync(
        Guid id,
        [NotNull] string name,
        [NotNull] string providerName,
        [NotNull] string providerKey);

    Task<PermissionGrant> UpdateAsync(Guid id,
        [NotNull] string name,
        [NotNull] string providerName,
        [NotNull] string providerKey);

    Task SetManyAsync(
        [NotNull] List<string> names,
        [NotNull] string providerName,
        [NotNull] string providerKey);

    Task RemoveAsync(Guid id);
}