using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;

public sealed class AppUserDto : AuditedEntityDto<Guid>
{
    public Guid TenantId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? LanguageCode { get; init; }

    public string? PhoneNumber { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    /// <summary>When the temporary password of the user runs out; nothing when the user has its own password.</summary>
    public DateTimeOffset? TemporaryPasswordExpiresAt { get; init; }
}
