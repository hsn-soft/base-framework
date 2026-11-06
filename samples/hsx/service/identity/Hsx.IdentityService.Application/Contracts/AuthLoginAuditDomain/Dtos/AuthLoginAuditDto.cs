using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos;

/// <summary>A sign-in that was tried and how it ended.</summary>
public sealed class AuthLoginAuditDto : CreationAuditedEntityDto<Guid>
{
    /// <summary>The tenant of the user; nothing when the user is not known.</summary>
    public Guid? TenantId { get; init; }

    public Guid? UserId { get; init; }

    /// <summary>What the caller typed: the user name or the email.</summary>
    public string UserNameOrEmail { get; init; } = string.Empty;

    public bool IsSuccess { get; init; }

    /// <summary>Why it failed (<c>unknown-user</c>, <c>wrong-password</c>, <c>locked-out</c>, <c>not-active</c>); nothing when it worked.</summary>
    public string? FailureReason { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}
