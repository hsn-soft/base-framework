namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;

    public bool IsSystemTenant { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AuthUser> Users { get; set; } = [];
    public ICollection<AuthRole> Roles { get; set; } = [];
}