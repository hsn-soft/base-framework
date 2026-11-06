using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos.Filters;

namespace Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Services;

/// <summary>
/// The sign-in audit log, read only. A caller reads the sign-ins of its own tenant and of the tenants under it (the system tenant reads all of them, the unknown users too), and only as far back as its role allows
/// (<c>constraint.audit-logs.max-days</c>; a caller whose roles have none is refused).
/// </summary>
public interface IAuthLoginAuditAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such audit, it is older than the caller may read, or it is not of the caller's tenants.</exception>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The caller has no limit of days (403).</exception>
    Task<AuthLoginAuditDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.BaseHttpException">The caller has no limit of days (403).</exception>
    Task<PagedDataResultDto<AuthLoginAuditDto>> GetPagedListAsync(GetAuthLoginAuditsPagedDto pagedInput, CancellationToken cancellationToken = default);
}
