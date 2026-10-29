using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HsnSoft.Base.Logging.Masking;

/// <summary>A value of a log as a tree a provider turns into its own: a plain value, an object with its properties, a dictionary or a list.</summary>
public abstract record MaskedNode;

/// <summary>A plain value (text, number, date, ...) or <see langword="null"/>.</summary>
public sealed record MaskedScalar(object? Value) : MaskedNode;

/// <summary>An object, with the name of its type and its properties (the sensitive ones already masked).</summary>
public sealed record MaskedStructure(string TypeName, IReadOnlyList<KeyValuePair<string, MaskedNode>> Properties) : MaskedNode;

/// <summary>A dictionary: its keys as text.</summary>
public sealed record MaskedDictionary(IReadOnlyList<KeyValuePair<string, MaskedNode>> Entries) : MaskedNode;

/// <summary>A list of values.</summary>
public sealed record MaskedSequence(IReadOnlyList<MaskedNode> Items) : MaskedNode;

/// <summary>
/// Turns an object that is logged (<c>{@Object}</c>) into a tree with what is secret already masked (a property marked <see cref="SensitiveDataAttribute"/>, by the mask it names), so every logging library writes the same thing
/// and none can forget a field. A reference to something already seen is written as <c>[CyclicRef:Type]</c>.
/// </summary>
public static class SensitiveDataTree
{
    public static MaskedNode Build(object? value) => Build(value, new HashSet<object>(ReferenceEqualityComparer.Instance));

    /// <summary>Whether a value is written as it is (a number, a text, a date, an id, an enum) and not taken apart.</summary>
    public static bool IsSimple(Type type)
        => type.IsPrimitive
           || type.IsEnum
           || type == typeof(string)
           || type == typeof(decimal)
           || type == typeof(DateTime)
           || type == typeof(DateTimeOffset)
           || type == typeof(Guid)
           || type == typeof(TimeSpan);

    private static MaskedNode Build(object? value, HashSet<object> visited)
    {
        if (value is null)
        {
            return new MaskedScalar(null);
        }

        Type type = value.GetType();
        if (IsSimple(type))
        {
            return new MaskedScalar(value);
        }

        if (!type.IsValueType && !visited.Add(value))
        {
            return new MaskedScalar($"[CyclicRef:{type.Name}]");
        }

        if (value is IDictionary dictionary)
        {
            List<KeyValuePair<string, MaskedNode>> entries = [];
            foreach (DictionaryEntry entry in dictionary)
            {
                entries.Add(new KeyValuePair<string, MaskedNode>(entry.Key.ToString() ?? string.Empty, Build(entry.Value, visited)));
            }

            return new MaskedDictionary(entries);
        }

        if (value is IEnumerable enumerable)
        {
            List<MaskedNode> items = [];
            foreach (object? item in enumerable)
            {
                items.Add(Build(item, visited));
            }

            return new MaskedSequence(items);
        }

        List<KeyValuePair<string, MaskedNode>> properties = [];
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
            {
                continue;
            }

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            SensitiveDataAttribute? sensitive = property.GetCustomAttribute<SensitiveDataAttribute>();
            properties.Add(new KeyValuePair<string, MaskedNode>(property.Name, sensitive is null ? Build(propertyValue, visited) : Mask(propertyValue, property.PropertyType, sensitive)));
        }

        return new MaskedStructure(type.Name, properties);
    }

    private static MaskedNode Mask(object? value, Type propertyType, SensitiveDataAttribute attribute)
    {
        if (value is null)
        {
            return new MaskedScalar(null);
        }

        if (propertyType == typeof(string))
        {
            return new MaskedScalar(MaskingHelper.MaskByAttribute(value.ToString(), attribute));
        }

        if (value is IDictionary dictionary)
        {
            List<KeyValuePair<string, MaskedNode>> entries = [];
            foreach (DictionaryEntry entry in dictionary)
            {
                entries.Add(new KeyValuePair<string, MaskedNode>(entry.Key.ToString() ?? string.Empty, new MaskedScalar(MaskingHelper.MaskByAttribute(entry.Value?.ToString(), attribute))));
            }

            return new MaskedDictionary(entries);
        }

        if (value is not string && value is IEnumerable enumerable && ElementType(propertyType) == typeof(string))
        {
            return new MaskedSequence([.. enumerable.Cast<object?>().Select(item => (MaskedNode)new MaskedScalar(MaskingHelper.MaskByAttribute(item?.ToString(), attribute)))]);
        }

        // anything else that carries the attribute: its text, masked
        return new MaskedScalar(MaskingHelper.MaskByAttribute(value.ToString(), attribute));
    }

    private static Type? ElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments().FirstOrDefault();
        }

        return type.GetInterfaces().FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments().FirstOrDefault();
    }
}
