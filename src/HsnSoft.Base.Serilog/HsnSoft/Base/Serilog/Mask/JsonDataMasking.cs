using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Force.DeepCloner;

namespace HsnSoft.Base.Serilog.Mask;

public static class JsonDataMasking
{
    public static T MaskSensitiveData<T>(T data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        T clone = data.DeepClone();

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        object? masked = MaskObjectRecursive(clone!, visited);

        return (T)masked!;
    }

    private static object? MaskObjectRecursive(object? instance, HashSet<object> visited)
    {
        if (instance is null)
            return null;

        var type = instance.GetType();

        if (IsSimpleType(type))
            return instance;

        if (!type.IsValueType)
        {
            if (visited.Contains(instance))
                return instance;

            visited.Add(instance);
        }

        if (instance is IDictionary dictionary)
        {
            MaskDictionaryEntries(dictionary, visited);
            return instance;
        }

        if (instance is IEnumerable enumerable && instance is not string)
        {
            MaskEnumerableItems(enumerable, visited);
            return instance;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
                continue;

            object? currentValue;
            try
            {
                currentValue = property.GetValue(instance);
            }
            catch
            {
                continue;
            }

            if (currentValue is null)
                continue;

            var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();

            if (sensitiveAttr != null)
            {
                if (property.PropertyType == typeof(string))
                {
                    if (property.CanWrite && !IsAnonymousType(property.DeclaringType))
                    {
                        string? masked = MaskingHelper.MaskByAttribute((string?)currentValue, sensitiveAttr);
                        property.SetValue(instance, masked);
                    }

                    continue;
                }

                if (TryMaskDictionaryStringString(instance, property, currentValue, sensitiveAttr))
                    continue;

                if (TryMaskEnumerableOfString(instance, property, currentValue, sensitiveAttr))
                    continue;
            }

            if (IsComplexClass(property.PropertyType))
            {
                MaskObjectRecursive(currentValue, visited);
                continue;
            }

            if (currentValue is IEnumerable childEnumerable && currentValue is not string)
            {
                MaskEnumerableItems(childEnumerable, visited);
                continue;
            }

            if (currentValue is IDictionary childDictionary)
            {
                MaskDictionaryEntries(childDictionary, visited);
            }
        }

        return instance;
    }

    private static void MaskEnumerableItems(IEnumerable enumerable, HashSet<object> visited)
    {
        foreach (var item in enumerable)
        {
            if (item is null)
                continue;

            var itemType = item.GetType();

            if (IsSimpleType(itemType))
                continue;

            MaskObjectRecursive(item, visited);
        }
    }

    private static void MaskDictionaryEntries(IDictionary dictionary, HashSet<object> visited)
    {
        var keys = new List<object?>();

        foreach (DictionaryEntry entry in dictionary)
            keys.Add(entry.Key);

        foreach (var key in keys)
        {
            if (key is null)
                continue;

            var value = dictionary[key];
            if (value is null)
                continue;

            var valueType = value.GetType();

            if (IsSimpleType(valueType))
                continue;

            MaskObjectRecursive(value, visited);
        }
    }

    private static bool TryMaskDictionaryStringString(
        object instance,
        PropertyInfo property,
        object currentValue,
        SensitiveDataAttribute attribute)
    {
        if (currentValue is not IDictionary<string, string> dict)
            return false;

        var masked = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var pair in dict)
        {
            masked[pair.Key] = MaskingHelper.MaskByAttribute(pair.Value, attribute);
        }

        if (property.CanWrite && !IsAnonymousType(property.DeclaringType))
        {
            property.SetValue(instance, masked);
        }

        return true;
    }

    private static bool TryMaskEnumerableOfString(
        object instance,
        PropertyInfo property,
        object currentValue,
        SensitiveDataAttribute attribute)
    {
        if (currentValue is string)
            return false;

        if (currentValue is not IEnumerable enumerable)
            return false;

        Type? elementType = GetEnumerableElementType(property.PropertyType);
        if (elementType != typeof(string))
            return false;

        var maskedValues = new List<string?>();

        foreach (var item in enumerable)
        {
            maskedValues.Add(MaskingHelper.MaskByAttribute(item?.ToString(), attribute));
        }

        if (!property.CanWrite || IsAnonymousType(property.DeclaringType))
            return true;

        object? converted = ConvertStringCollection(maskedValues, property.PropertyType);
        if (converted != null)
        {
            property.SetValue(instance, converted);
        }

        return true;
    }

    private static object? ConvertStringCollection(List<string?> values, Type targetType)
    {
        if (targetType.IsArray && targetType.GetElementType() == typeof(string))
            return values.ToArray();

        if (targetType == typeof(List<string>) || targetType.IsAssignableFrom(typeof(List<string>)))
            return values.Select(x => x ?? string.Empty).ToList();

        if (targetType == typeof(IEnumerable<string>) || targetType.IsAssignableFrom(typeof(List<string>)))
            return values.Select(x => x ?? string.Empty).ToList();

        return null;
    }

    private static bool IsComplexClass(Type type)
    {
        return type.IsClass
               && type != typeof(string)
               && !typeof(IEnumerable).IsAssignableFrom(type)
               && !typeof(IDictionary).IsAssignableFrom(type);
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid);
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
            return type.GetGenericArguments().FirstOrDefault();

        var enumerableInterface = type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }

    private static bool IsAnonymousType(Type? type)
    {
        if (type == null)
            return false;

        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
               && type.IsGenericType
               && type.Name.Contains("AnonymousType", StringComparison.OrdinalIgnoreCase)
               && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase)
                   || type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}