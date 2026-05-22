using HsnSoft.Base.Domain.Entities;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AppUserRole : Entity<Guid>
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;


    private AppUserRole()
    {
    }

    internal AppUserRole(Guid userId, Guid roleId) : this(Guid.CreateVersion7(), userId, roleId)
    {
    }

    internal AppUserRole(Guid id, Guid userId, Guid roleId) : this()
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
    }
}