using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public abstract class PermissionValueProvider(IPermissionStore permissionStore) : IPermissionValueProvider, ITransientDependency
{
    protected IPermissionStore PermissionStore { get; } = permissionStore;
    public abstract string Name { get; }

    public abstract Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context);
}