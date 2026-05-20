using HsnSoft.Base.Domain.Entities;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AppRoleClaim: Entity<Guid>
{
    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;

    public string ClaimType { get; set; } = null!;
    public string ClaimValue { get; set; } = null!;
}