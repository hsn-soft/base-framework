using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;

namespace Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;

/// <summary>A sign-in that was tried, and how it ended: who (the user name or email the caller gave, and the user and its tenant when there is one), whether it worked, why not, and where from. It is written for every attempt and never changed.</summary>
public sealed class AuthLoginAudit : CreationAuditedEntity<Guid>
{
    // for EF Core
    private AuthLoginAudit()
    {
    }

    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public string UserNameOrEmail { get; private set; } = string.Empty;

    public bool IsSuccess { get; private set; }

    public string? FailureReason { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public static AuthLoginAudit Record(string userNameOrEmail, Guid? tenantId, Guid? userId, bool isSuccess, string? failureReason, string? ipAddress, string? userAgent, Guid? id = null)
        => new()
        {
            Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
            UserNameOrEmail = Guard.NotNullOrWhiteSpace(userNameOrEmail, nameof(UserNameOrEmail), AuthLoginAuditConsts.UserNameOrEmailMaxLength).Trim(),
            IsSuccess = isSuccess,
            FailureReason = failureReason,
            IpAddress = Cut(ipAddress, AuthLoginAuditConsts.IpAddressMaxLength),
            UserAgent = Cut(userAgent, AuthLoginAuditConsts.UserAgentMaxLength),
        };

    // what comes from the caller is cut, not refused: an audit that cannot be written because of a long header is a hole in the audit
    private static string? Cut(string? value, int length) => value is null || value.Length <= length ? value : value[..length];
}
