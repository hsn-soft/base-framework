using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.Store;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public abstract class PermissionConstraintValueProvider(IPermissionConstraintStore permissionConstraintStore) : IPermissionConstraintValueProvider
{
    protected IPermissionConstraintStore PermissionConstraintStore { get; } = permissionConstraintStore;
    public abstract string Name { get; }

    public abstract Task<PermissionConstraintValueResult> CheckAsync(PermissionConstraintCheckContext context);
}