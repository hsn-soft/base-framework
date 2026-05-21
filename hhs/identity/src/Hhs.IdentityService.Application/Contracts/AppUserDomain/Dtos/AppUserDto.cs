using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;

public sealed class AppUserDto
{
    // Default identity model
    public Guid Id { get; set; }

    [CanBeNull]
    public string UserName { get; set; }

    [CanBeNull]
    public string Email { get; set; }

    public bool EmailConfirmed { get; set; }

    [CanBeNull]
    public string PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    // public DateTimeOffset? LockoutEnd { get; set; }
    // public bool LockoutEnabled { get; set; }
    // public int AccessFailedCount { get; set; }

    // Custom identity model
    public Guid? TenantId { get; set; }
    [CanBeNull]
    public string TenantDomain { get; set; }

    [CanBeNull]
    public string Name { get; set; }

    [CanBeNull]
    public string Surname { get; set; }

    [CanBeNull]
    public string DefaultLanguage { get; set; }
    [CanBeNull]
    public string AvatarSuffixUrl { get;  set; }

    [CanBeNull]
    public List<string> Roles { get; set; }
}