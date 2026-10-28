using System.ComponentModel.DataAnnotations;

namespace Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;

/// <summary>What a role or a user is granted from now on: exactly these, nothing else (the ones it had are replaced).</summary>
public sealed class SetGrantsDto
{
    [Required]
    [MaxLength(1000)]
    public List<GrantInputDto> Grants { get; set; } = [];
}

public sealed class GrantInputDto
{
    /// <summary>The code of the permission (<c>endpoint.intake.files.get</c>).</summary>
    [Required]
    [StringLength(300)]
    public string Code { get; set; } = string.Empty;

    /// <summary>The value of a constraint (<c>20971520</c>); it must be given for a constraint and left out for any other permission.</summary>
    [StringLength(256)]
    public string? Value { get; set; }
}

public sealed class UserCreateDto
{
    [Required]
    [StringLength(64)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    [HsnSoft.Base.Logging.Masking.SensitiveData]
    public string Password { get; set; } = string.Empty;

    /// <summary>The roles the user has (roles of its tenant).</summary>
    public List<Guid> RoleIds { get; set; } = [];
}

public sealed class UserUpdateDto
{
    [Required]
    public Guid? Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>The roles the user has from now on: exactly these.</summary>
    public List<Guid> RoleIds { get; set; } = [];
}

public sealed class RoleCreateDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;
}
