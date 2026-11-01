using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Logging.Masking;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;

public sealed class ChangePasswordDto
{
    [Required]
    [SensitiveData]
    public string? CurrentPassword { get; set; }

    [Required]
    [SensitiveData]
    [StringLength(AppUserConsts.PasswordMaxLength, MinimumLength = AppUserConsts.PasswordMinLength)]
    public string? NewPassword { get; set; }
}
