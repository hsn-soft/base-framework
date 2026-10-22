using System.Collections.Generic;
using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization.Permissions.Store;

public interface IPermissionStore
{
    Task<bool> IsGrantedAsync(
        string permission,
        string providerName,
        string providerKey);

    Task SetAllPermissions(IEnumerable<PermissionAssignment> permissions);

    Task<IEnumerable<PermissionAssignment>> GetAllPermissions();
}