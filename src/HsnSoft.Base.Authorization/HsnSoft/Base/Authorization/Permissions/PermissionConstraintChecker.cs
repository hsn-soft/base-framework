using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.ValueProviders;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions;

public class PermissionConstraintChecker(
    IEnumerable<IPermissionConstraintValueProvider> providers,
    ICurrentPrincipalAccessor principalAccessor
) : IPermissionConstraintChecker
{
    public async Task<string> GetValueAsync(string constraint)
    {
        var principal = principalAccessor.Principal;

        foreach (var provider in providers)
        {
            var result = await provider.CheckAsync(new PermissionConstraintCheckContext(constraint, principal));

            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return null;
    }

    public async Task<decimal?> GetDecimalAsync(string constraint)
    {
        string value = await GetValueAsync(constraint);

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return null;
    }

    public async Task<int?> GetIntAsync(string constraint)
    {
        string value = await GetValueAsync(constraint);

        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return null;
    }

    public async Task<bool?> GetBooleanAsync(string constraint)
    {
        string value = await GetValueAsync(constraint);

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        return null;
    }
}