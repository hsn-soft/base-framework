using System.ComponentModel.DataAnnotations;
using Abc.IdentityService.Domain.UserDomain.Consts;
using HsnSoft.Base.Logging.Masking;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;

public sealed class UserCreateDto
{
    [Required]
    [StringLength(UserConsts.UserNameMaxLength)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(UserConsts.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(UserConsts.DisplayNameMaxLength)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(UserConsts.PasswordMaxLength, MinimumLength = UserConsts.PasswordMinLength)]
    [SensitiveData]
    public string Password { get; set; } = string.Empty;

    /// <summary>The roles (<c>admin</c>, <c>customer</c>); <c>customer</c> when none is given.</summary>
    public List<string>? Roles { get; set; }
}
