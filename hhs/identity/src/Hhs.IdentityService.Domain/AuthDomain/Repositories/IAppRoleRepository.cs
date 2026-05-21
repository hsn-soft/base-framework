using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Repositories;

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