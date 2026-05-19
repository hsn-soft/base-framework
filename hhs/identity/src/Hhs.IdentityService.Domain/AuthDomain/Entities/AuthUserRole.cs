namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthUserRole
{
    public Guid UserId { get; set; }
    public AuthUser User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public AuthRole Role { get; set; } = null!;
}