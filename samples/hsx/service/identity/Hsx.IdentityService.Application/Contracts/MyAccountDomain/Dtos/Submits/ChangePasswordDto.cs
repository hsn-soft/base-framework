using System.ComponentModel.DataAnnotations;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using HsnSoft.Base.Logging.Masking;

namespace Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos.Submits;

/// <summary>The password of the caller changes: the current one (the temporary one that was sent included) and the new one, which keeps the rules of the tenant.</summary>
public sealed class ChangePasswordDto
{
    [Required]
    [SensitiveData]
    public string? CurrentPassword { get; set; }

    [Required]
    [StringLength(AppUserConsts.PasswordMaxLength, MinimumLength = AppUserConsts.PasswordMinLength)]
    [SensitiveData]
    public string? NewPassword { get; set; }
}
