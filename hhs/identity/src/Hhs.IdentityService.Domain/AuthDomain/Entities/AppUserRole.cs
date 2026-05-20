using HsnSoft.Base.Domain.Entities;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AppUserRole: Entity<Guid>
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;
}