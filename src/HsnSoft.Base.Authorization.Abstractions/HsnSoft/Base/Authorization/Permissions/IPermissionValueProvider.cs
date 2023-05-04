using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization.Permissions;

public interface IPermissionValueProvider
{
    string Name { get; }

    //TODO: Rename to GetResult? (CheckAsync throws exception by naming convention)
    Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context);
}