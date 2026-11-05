using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;

/// <summary>The password of the caller was changed and its sessions ended: it signs in again with the new one.</summary>
public sealed class PasswordChangedDto : IBaseDto
{
    public DateTimeOffset ChangedAt { get; init; }
}
