using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.AuthServer.Domain.UserDomain.Consts;

namespace Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Submits;

internal static class UserRoleRules
{
    /// <summary>What an attribute cannot say about the roles: each is a word (blank ones are refused by <c>[NoBlankItems]</c>) of at most <see cref="UserConsts.RoleMaxLength"/> characters without a comma (the domain keeps the same rule as its last guard).</summary>
    public static IEnumerable<ValidationResult> Check(IEnumerable<string>? roles)
    {
        int index = 0;
        foreach (string? role in roles ?? [])
        {
            if (role is null || role.Trim().Length > UserConsts.RoleMaxLength || role.Contains(','))
            {
                yield return ValidationRule.Failed("Roles", "Validation:InvalidValue", $"Roles[{index}]");
            }

            index++;
        }
    }
}
