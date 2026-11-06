using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos.Filters;

public sealed class GetAuthLoginAuditsPagedDto : PagedDataRequestDto
{
    /// <summary>Only the sign-ins of this user.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Only the sign-ins of the users of this tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Only the sign-ins that worked (<see langword="true"/>) or only the ones that failed (<see langword="false"/>); all when it is not given.</summary>
    public bool? IsSuccess { get; set; }

    /// <summary>From this moment on. The role's limit (<c>constraint.audit-logs.max-days</c>) still applies: an earlier moment is read as the limit.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Up to this moment.</summary>
    public DateTimeOffset? To { get; set; }
}
