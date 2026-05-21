using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Repositories;

public interface IAppRoleRepository: IReadOnlyGenericRepository<AppRole, Guid>
{
    Task<AppRole> CreateAsync(Guid tenantId, [NotNull] string tenantDomain,
        [NotNull] string name,
        bool isDefault,
        bool isStatic,
        bool isPublic);

    Task<AppRole> CreateAsync(Guid id, Guid tenantId, [NotNull] string tenantDomain,
        [NotNull] string name,
        bool isDefault,
        bool isStatic,
        bool isPublic);


    Task<AppRole> UpdateAsync(Guid id,
        [NotNull] string name,
        bool isDefault,
        bool isStatic,
        bool isPublic);

    Task DeleteAsync(Guid id);
}