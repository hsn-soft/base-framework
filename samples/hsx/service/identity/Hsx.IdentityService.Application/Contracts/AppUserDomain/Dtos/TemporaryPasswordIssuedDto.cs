using HsnSoft.Base.Application.Dtos;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;

/// <summary>A new temporary password was made for a user and sent to it. The password is not here: no person sees it.</summary>
public sealed class TemporaryPasswordIssuedDto : IBaseDto
{
    public Guid UserId { get; init; }

    /// <summary>How the user was told.</summary>
    public NotificationTypes NotificationType { get; init; }

    public DateTimeOffset PasswordExpiresAt { get; init; }
}
