using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/audit-logs/logins")]
public sealed class AuthLoginAuditsController(IAuthLoginAuditAppService loginAudits) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AuditLogs.Read)]
    public Task<AuthLoginAuditDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => loginAudits.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.AuditLogs.Read)]
    public Task<PagedDataResultDto<AuthLoginAuditDto>> GetPagedListAsync([FromBody] GetAuthLoginAuditsPagedDto pagedInput, CancellationToken cancellationToken = default)
        => loginAudits.GetPagedListAsync(pagedInput, cancellationToken);
}
