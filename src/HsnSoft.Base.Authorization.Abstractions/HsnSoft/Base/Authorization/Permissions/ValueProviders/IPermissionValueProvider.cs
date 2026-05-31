using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public interface IPermissionValueProvider
{
    string Name { get; }

    Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context);
}