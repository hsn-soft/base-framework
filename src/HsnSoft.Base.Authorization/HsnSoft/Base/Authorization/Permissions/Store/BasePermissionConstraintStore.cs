using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Authorization.Permissions.Store;

public class BasePermissionConstraintStore : IPermissionConstraintStore, ISingletonDependency
{
    private readonly Lock _lock = new();
    private Dictionary<string, string> _constraintValues = new();
    private IReadOnlyCollection<PermissionConstraintAssignment> _constraints = new List<PermissionConstraintAssignment>();

    private static string BuildKey(string constraint, string providerName, string providerKey)
    {
        constraint ??= string.Empty;
        providerName ??= string.Empty;
        providerKey ??= string.Empty;

        string value = $"{providerName}|{providerKey}|{constraint}";

        return string.Join("", value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }

    public Task<string?> GetValueAsync(string constraint, string providerName, string providerKey)
    {
        if (string.IsNullOrWhiteSpace(constraint) || string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(providerKey))
        {
            return Task.FromResult<string?>(null);
        }

        string key = BuildKey(constraint, providerName, providerKey);

        _constraintValues.TryGetValue(key, out string? value);

        return Task.FromResult(value);
    }

    public Task SetAllConstraints(IEnumerable<PermissionConstraintAssignment> constraints)
    {
        constraints ??= [];

        var constraintList = constraints.ToList();

        var values = constraintList.ToDictionary(
            x => BuildKey(
                x.Constraint,
                x.ProviderName,
                x.ProviderKey),
            x => x.Value);

        lock (_lock)
        {
            _constraints = constraintList;
            _constraintValues = values;
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<PermissionConstraintAssignment>> GetAllConstraints()
    {
        return Task.FromResult<IEnumerable<PermissionConstraintAssignment>>(_constraints);
    }
}