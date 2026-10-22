using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public interface IPermissionConstraintValueProvider
{
    string Name { get; }

    Task<PermissionConstraintValueResult> CheckAsync(PermissionConstraintCheckContext context);
}