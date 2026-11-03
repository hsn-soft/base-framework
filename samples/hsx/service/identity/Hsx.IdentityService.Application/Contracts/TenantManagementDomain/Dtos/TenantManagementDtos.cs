using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Logging.Masking;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.Shared.Helper;

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

public sealed class CreateCompanyDto
{
    [Required]
    [StringLength(TenantConsts.TitleMaxLength)]
    public string? Title { get; set; }

    [Required]
    [StringLength(TenantConsts.NameMaxLength)]
    public string? Name { get; set; }

    /// <summary>The reseller the company is made for. Only the system tenant names one; a reseller makes companies for itself.</summary>
    public Guid? ResellerTenantId { get; set; }

    public AdminUserInputDto? AdminUser { get; set; }
}

/// <summary>What was made.</summary>
public sealed class ProvisionedTenantDto
{
    public Guid TenantId { get; init; }

    /// <summary>The role <c>Admin</c> of the tenant.</summary>
    public Guid RoleId { get; init; }

    public Guid? UserId { get; init; }

    public Guid? CompanyId { get; init; }
}

public sealed class CreateCustomerDto
{
    [Required]
    public Guid? CompanyId { get; set; }

    /// <summary>The website of the customer (<c>news.example.com</c>).</summary>
    [Required]
    [StringLength(253)]
    public string? Domain { get; set; }
}

public sealed class SellSubscriptionDto
{
    [Required]
    public Guid? CustomerId { get; set; }

    [Required]
    public ProductTypes? ProductType { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    /// <summary>The reseller that sells. Only the system tenant names one; a reseller sells as itself.</summary>
    public Guid? TenantId { get; set; }
}
