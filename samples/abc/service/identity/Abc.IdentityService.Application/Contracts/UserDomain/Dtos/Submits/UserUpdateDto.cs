using System.ComponentModel.DataAnnotations;
using Abc.IdentityService.Domain.UserDomain.Consts;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;

public sealed class UserUpdateDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(UserConsts.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(UserConsts.DisplayNameMaxLength)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    /// <summary>The roles of the user after the change (<c>customer</c> when none is given).</summary>
    public List<string>? Roles { get; set; }

    /// <summary>The language the user is answered in; <c>en</c> when none is given.</summary>
    [StringLength(UserConsts.LanguageMaxLength)]
    public string? Language { get; set; }
}
