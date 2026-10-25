using System.ComponentModel.DataAnnotations;
using Abc.IdentityService.Domain.UserDomain.Consts;
using HsnSoft.Base;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;

internal static class UserRoleRules
{
    /// <summary>What an attribute cannot say about the roles: each is a word of at most <see cref="UserConsts.RoleMaxLength"/> characters without a comma (the domain keeps the same rule as its last guard).</summary>
    public static IEnumerable<ValidationResult> Check(IEnumerable<string>? roles)
    {
        int index = 0;
        foreach (string? role in roles ?? [])
        {
            if (string.IsNullOrWhiteSpace(role) || role.Trim().Length > UserConsts.RoleMaxLength || role.Contains(','))
            {
                yield return ValidationRule.Failed("Roles", "Validation:InvalidValue", $"Roles[{index}]");
            }

            index++;
        }
    }
}
