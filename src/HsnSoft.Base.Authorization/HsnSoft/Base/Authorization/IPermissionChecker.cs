using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization;

/// <summary>Asks what the current caller was granted. Use it in code that needs more than the <see cref="PermissionAuthorizeAttribute"/> of an endpoint: an operation inside an action, a value that limits what the caller may do.</summary>
public interface IPermissionChecker
{
    /// <summary>Whether the caller, or one of its roles, was granted the permission. A caller that is not signed in was granted nothing.</summary>
    Task<bool> IsGrantedAsync(string permission, CancellationToken cancellationToken = default);

    /// <summary>The value of a permission that limits something (a constraint): the user's own is first, then the roles' in the order the token has them; <see langword="null"/> when none has one.</summary>
    Task<string?> GetValueAsync(string permission, CancellationToken cancellationToken = default);
}
