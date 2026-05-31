using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.Authorization.Permissions.ValueProviders;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions;

public class PermissionConstraintChecker(IPermissionConstraintStore store, ICurrentPrincipalAccessor principalAccessor) : IPermissionConstraintChecker
{
    public async Task<string> GetValueAsync(string constraint)
    {
        var principal = principalAccessor.Principal;

        string[] roles = principal?.FindAll(BaseClaimTypes.Role).Select(x => x.Value).ToArray() ?? [];

        foreach (string role in roles)
        {
            string value = await store.GetValueAsync(constraint, PermissionProviders.Role, role);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
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