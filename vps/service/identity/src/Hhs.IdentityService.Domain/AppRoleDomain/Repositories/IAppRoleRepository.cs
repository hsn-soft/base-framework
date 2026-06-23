using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Repositories;

public interface IAppRoleRepository: IReadOnlyGenericRepository<AppRole, Guid>
{
    Task<AppRole> CreateAsync(Guid tenantId,
        [NotNull] string name,
        bool isDefault = false,
        bool isStatic = false);

    Task<AppRole> CreateAsync(Guid id, Guid tenantId,
        [NotNull] string name,
        bool isDefault = false,
        bool isStatic = false);


    Task<AppRole> UpdateAsync(Guid id,
        [NotNull] string name,
        bool isDefault = false,
        bool isStatic = false);

    Task DeleteAsync(Guid id);
}