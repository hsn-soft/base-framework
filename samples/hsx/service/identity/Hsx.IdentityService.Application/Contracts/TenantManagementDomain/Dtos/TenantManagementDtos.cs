using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Logging.Masking;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.IdentityService.Domain.TenantDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;

/// <summary>The first administrator of a tenant that is made: a user of that tenant with the role <c>Admin</c>.</summary>
public sealed class AdminUserInputDto
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
    [SensitiveData]
    [StringLength(AppUserConsts.PasswordMaxLength, MinimumLength = AppUserConsts.PasswordMinLength)]
    public string? Password { get; set; }

    [StringLength(AppUserConsts.LanguageCodeMaxLength)]
    public string? LanguageCode { get; set; }
}

public sealed class CreateResellerDto
{
    [Required]
    [StringLength(TenantConsts.TitleMaxLength)]
    public string? Title { get; set; }

    /// <summary>What the tenant is called in lists and paths (small letters, unique).</summary>
    [Required]
    [StringLength(TenantConsts.NameMaxLength)]
    public string? Name { get; set; }

    public AdminUserInputDto? AdminUser { get; set; }
}

/// <summary>What was made.</summary>
public sealed class ProvisionedTenantDto
{
    public Guid TenantId { get; init; }

    /// <summary>The role <c>Admin</c> of the tenant (a Tenant role).</summary>
    public Guid RoleId { get; init; }

    public Guid? UserId { get; init; }
}
