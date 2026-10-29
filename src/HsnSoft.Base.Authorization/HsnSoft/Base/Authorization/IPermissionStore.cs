using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization;

/// <summary>
/// Where the grants are kept for the services to read: one entry for each subject in a scope. The service that manages the grants writes them (<see cref="SetAsync"/>), every other service only reads. A subject that has no entry has been
/// granted nothing.
/// </summary>
public interface IPermissionStore
{
    Task<PermissionGrants> GetAsync(string? scope, PermissionSubject subject, CancellationToken cancellationToken = default);

    /// <summary>Replaces what the subject was granted.</summary>
    Task SetAsync(string? scope, PermissionSubject subject, PermissionGrants grants, CancellationToken cancellationToken = default);

    /// <summary>Takes the entry of the subject away (it has been granted nothing, and is not known any more).</summary>
    Task RemoveAsync(string? scope, PermissionSubject subject, CancellationToken cancellationToken = default);
}
