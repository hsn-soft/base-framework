using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Authorization.Permissions.Store;

public class BasePermissionStore : IPermissionStore, ISingletonDependency
{
    private readonly Lock _lock = new();
    private HashSet<string> _permissionKeys = new();
    private IReadOnlyCollection<PermissionAssignment> _permissions = new List<PermissionAssignment>();

    private static string BuildKey(string permission, string providerName, string providerKey)
    {
        permission ??= string.Empty;
        providerName ??= string.Empty;
        providerKey ??= string.Empty;

        string value = $"{providerName}|{providerKey}|{permission}";

        return string.Join("", value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }

    public Task<bool> IsGrantedAsync(string permission, string providerName, string providerKey)
    {
        if (string.IsNullOrWhiteSpace(permission) || string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(providerKey))
        {
            return Task.FromResult(false);
        }

        string key = BuildKey(permission, providerName, providerKey);

        bool granted = _permissionKeys.Contains(key);

        return Task.FromResult(granted);
    }

    public Task SetAllPermissions(IEnumerable<PermissionAssignment> permissions)
    {
        permissions ??= [];

        var permissionList = permissions.ToList();

        var permissionKeys = permissionList
            .Select(x => BuildKey(
                x.Permission,
                x.ProviderName,
                x.ProviderKey))
            .ToHashSet();

        lock (_lock)
        {
            _permissions = permissionList;
            _permissionKeys = permissionKeys;
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<PermissionAssignment>> GetAllPermissions()
    {
        return Task.FromResult<IEnumerable<PermissionAssignment>>(_permissions);
    }
}