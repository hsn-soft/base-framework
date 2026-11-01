using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;

public sealed class AppUserCreateDto
{
    [Required]
    [StringLength(AppUserConsts.UserNameMaxLength)]
    public string? UserName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(AppUserConsts.EmailMaxLength)]
    public string? Email { get; set; }

    [Required]
    [StringLength(AppUserConsts.DisplayNameMaxLength)]
    public string? DisplayName { get; set; }

    [Required]
    [StringLength(AppUserConsts.PasswordMaxLength, MinimumLength = AppUserConsts.PasswordMinLength)]
    public string? Password { get; set; }

    [StringLength(AppUserConsts.LanguageCodeMaxLength)]
    public string? LanguageCode { get; set; }
}

/// <summary>What can be changed of a user (the user name stays, the password has its own call).</summary>
public sealed class AppUserUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(AppUserConsts.EmailMaxLength)]
    public string? Email { get; set; }

    [Required]
    [StringLength(AppUserConsts.DisplayNameMaxLength)]
    public string? DisplayName { get; set; }

    [StringLength(AppUserConsts.LanguageCodeMaxLength)]
    public string? LanguageCode { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class AppUserSetPasswordDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(AppUserConsts.PasswordMaxLength, MinimumLength = AppUserConsts.PasswordMinLength)]
    public string? Password { get; set; }
}

/// <summary>Gives a user exactly these roles (roles of its own tenant).</summary>
public sealed class AppUserSetRolesDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    public List<Guid>? RoleIds { get; set; }
}
