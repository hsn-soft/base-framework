using System.Collections.Generic;
using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization.Permissions.Store;

public interface IPermissionConstraintStore
{
    Task<string?> GetValueAsync(
        string constraint,
        string providerName,
        string providerKey);

    Task SetAllConstraints(IEnumerable<PermissionConstraintAssignment> constraints);

    Task<IEnumerable<PermissionConstraintAssignment>> GetAllConstraints();
}