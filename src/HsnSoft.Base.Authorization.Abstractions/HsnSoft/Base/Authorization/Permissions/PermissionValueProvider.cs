using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Authorization.Permissions;

public abstract class PermissionValueProvider : IPermissionValueProvider, ITransientDependency
{
    protected PermissionValueProvider(IPermissionStore permissionStore)
    {
        PermissionStore = permissionStore;
    }

    protected IPermissionStore PermissionStore { get; }
    public abstract string Name { get; }

    public abstract Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context);
}