using System.Security.Claims;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HsnSoft.Base.Authorization.Permissions;

public interface IPermissionChecker
{
    Task<bool> IsGrantedAsync([NotNull] string name);

    Task<bool> IsGrantedAsync([CanBeNull] ClaimsPrincipal claimsPrincipal, [NotNull] string name);
}