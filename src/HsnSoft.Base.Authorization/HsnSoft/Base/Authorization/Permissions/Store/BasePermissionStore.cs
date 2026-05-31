using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Authorization.Permissions.Store;

public class BasePermissionStore : IPermissionStore, ISingletonDependency
{
    private ImmutableHashSet<string> _permissionKeys = ImmutableHashSet<string>.Empty;
    private IReadOnlyCollection<PermissionAssignment> _permissions = [];

    private static string BuildKey(string permission, string providerName, string providerKey)
    {
        permission ??= string.Empty;
        providerName ??= string.Empty;
        providerKey ??= string.Empty;

        string value = $"{providerName}|{providerKey}|{permission}";

        return string.Join(
            "",
            value.Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD)
                .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }

    public Task<bool> IsGrantedAsync(string permission, string providerName, string providerKey)
    {
        if (string.IsNullOrWhiteSpace(permission) ||
            string.IsNullOrWhiteSpace(providerName) ||
            string.IsNullOrWhiteSpace(providerKey))
        {
            return Task.FromResult(false);
        }

        string key = BuildKey(permission, providerName, providerKey);

        return Task.FromResult(_permissionKeys.Contains(key));
    }

    public Task SetAllPermissions(IEnumerable<PermissionAssignment> permissions)
    {
        permissions ??= [];

        var permissionList = permissions.ToList();

        _permissions = permissionList;

        _permissionKeys = permissionList
            .Select(x => BuildKey(
                x.Permission,
                x.ProviderName,
                x.ProviderKey))
            .ToImmutableHashSet();

        return Task.CompletedTask;
    }

    public Task<IEnumerable<PermissionAssignment>> GetAllPermissions()
    {
        return Task.FromResult<IEnumerable<PermissionAssignment>>(_permissions);
    }
}