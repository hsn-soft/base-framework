using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace HsnSoft.Base.Logging.Masking;

/// <summary>
/// Finds, in a class, the properties that carry <see cref="SensitiveDataAttribute"/> and the name they have in JSON. An application
/// has thousands of them and says which in its classes, where they are declared, not in a settings file.
/// </summary>
public static class SensitiveDataShape
{
    private const int MaxDepth = 6;

    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, SensitiveDataAttribute>> s_shapes = new();

    /// <summary>The sensitive properties of <paramref name="type"/> and of the classes it contains (lists, dictionaries and nested objects included), by JSON name (case does not matter).</summary>
    public static IReadOnlyDictionary<string, SensitiveDataAttribute> Of(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return s_shapes.GetOrAdd(type, static key =>
        {
            Dictionary<string, SensitiveDataAttribute> found = new(StringComparer.OrdinalIgnoreCase);
            Collect(key, 0, [], found);
            return found;
        });
    }

    /// <summary>The sensitive properties of every public class of the assembly: the contracts of a system, for a place that sees the JSON but not the classes (a gateway).</summary>
    public static IReadOnlyDictionary<string, SensitiveDataAttribute> Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Dictionary<string, SensitiveDataAttribute> found = new(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in assembly.GetExportedTypes().Where(candidate => candidate.IsClass))
        {
            foreach (KeyValuePair<string, SensitiveDataAttribute> pair in Of(type))
            {
                found.TryAdd(pair.Key, pair.Value);
            }
        }

        return found;
    }

    private static void Collect(Type type, int depth, HashSet<Type> visited, Dictionary<string, SensitiveDataAttribute> found)
    {
        Type? element = Content(type);
        if (element is null || depth > MaxDepth || !visited.Add(element))
        {
            return;
        }

        foreach (PropertyInfo property in element.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(candidate => candidate.GetIndexParameters().Length == 0))
        {
            SensitiveDataAttribute? attribute = property.GetCustomAttribute<SensitiveDataAttribute>();
            if (attribute is not null)
            {
                found.TryAdd(property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name, attribute);
                continue;
            }

            Collect(property.PropertyType, depth + 1, visited, found);
        }
    }

    // the class that is worth looking into: a list of orders is Order, an optional Address is Address; text, numbers and platform types have nothing
    private static Type? Content(Type type)
    {
        Type current = Nullable.GetUnderlyingType(type) ?? type;
        if (current.IsArray)
        {
            current = current.GetElementType()!;
        }
        else if (current != typeof(string) && typeof(IEnumerable).IsAssignableFrom(current))
        {
            Type? generic = current.IsGenericType ? current.GetGenericArguments().Last() : current.GetInterfaces().FirstOrDefault(face => face.IsGenericType && face.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
            if (generic is null)
            {
                return null;
            }

            current = generic;
        }

        // a nested generic envelope (BaseResponse<Order>) is looked into as itself and through its arguments
        return current.IsClass && current != typeof(string) && !IsPlatform(current) ? current : null;
    }

    private static bool IsPlatform(Type type)
        => type.Namespace is { } ns && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal) || ns.StartsWith("Microsoft.", StringComparison.Ordinal));
}
