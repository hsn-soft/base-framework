using System.ComponentModel.DataAnnotations;
using Abc.IdentityService.Domain.UserDomain.Consts;
using HsnSoft.Base;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;

public sealed class UserUpdateDto : IValidatableObject
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(UserConsts.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(UserConsts.DisplayNameMaxLength)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public bool? IsActive { get; set; }

    /// <summary>The roles of the user after the change (<c>customer</c> when none is given).</summary>
    [NoBlankItems]
    public List<string>? Roles { get; set; }

    /// <summary>The language the user is answered in; <c>en</c> when none is given.</summary>
    [StringLength(UserConsts.LanguageMaxLength)]
    public string? Language { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => UserRoleRules.Check(Roles);
}
