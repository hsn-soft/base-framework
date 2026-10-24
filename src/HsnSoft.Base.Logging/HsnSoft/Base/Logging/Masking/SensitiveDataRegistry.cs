using System;
using System.Collections.Generic;
using System.Reflection;

namespace HsnSoft.Base.Logging.Masking;

/// <summary>
/// The sensitive properties (<see cref="SensitiveDataAttribute"/>) of the contract classes an application registered, so a place that
/// only sees JSON (the request log of a gateway) hides them too. Filled from assemblies at registration (<c>AddBaseLogging</c>).
/// </summary>
public sealed class SensitiveDataRegistry
{
    private readonly Dictionary<string, SensitiveDataAttribute> _keys = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, SensitiveDataAttribute> Keys => _keys;

    public SensitiveDataRegistry Add(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Merge(SensitiveDataShape.Of(assembly));
        return this;
    }

    public SensitiveDataRegistry Add(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Merge(SensitiveDataShape.Of(type));
        return this;
    }

    private void Merge(IReadOnlyDictionary<string, SensitiveDataAttribute> keys)
    {
        foreach (KeyValuePair<string, SensitiveDataAttribute> pair in keys)
        {
            _keys.TryAdd(pair.Key, pair.Value);
        }
    }
}
