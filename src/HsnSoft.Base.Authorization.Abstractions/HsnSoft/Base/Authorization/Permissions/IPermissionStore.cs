using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HsnSoft.Base.Authorization.Permissions;

public interface IPermissionStore
{
    Task<bool> IsGrantedAsync(
        [NotNull] string name,
        [CanBeNull] string providerName,
        [CanBeNull] string providerKey
    );

    Task SetAllPermissions(IEnumerable<BasePermissionStoreItem> permissions);

    Task<IEnumerable<BasePermissionStoreItem>> GetAllPermissions();
}