using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;

/// <summary>What can be changed of a user (the user name stays; the password has its own call).</summary>
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

    [StringLength(AppUserConsts.PhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }

    [StringLength(AppUserConsts.LanguageCodeMaxLength)]
    public string? LanguageCode { get; set; }

    public bool IsActive { get; set; } = true;
}
