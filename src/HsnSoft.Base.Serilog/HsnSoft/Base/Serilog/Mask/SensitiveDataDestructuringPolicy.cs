using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Mask;

public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        result = DestructureValue(
            value,
            propertyValueFactory,
            new HashSet<object>(ReferenceEqualityComparer.Instance));

        return true;
    }

    private LogEventPropertyValue DestructureValue(
        object? value,
        ILogEventPropertyValueFactory factory,
        HashSet<object> visited)
    {
        if (value is null)
            return new ScalarValue(null);

        var type = value.GetType();

        if (IsSimpleType(type))
            return factory.CreatePropertyValue(value, destructureObjects: false);

        if (!type.IsValueType)
        {
            if (!visited.Add(value))
                return new ScalarValue($"[CyclicRef:{type.Name}]");
        }

        if (TryHandleDictionary(value, factory, visited, out var dictionaryResult))
            return dictionaryResult;

        if (TryHandleEnumerable(value, factory, visited, out var enumerableResult))
            return enumerableResult;

        var properties = new List<LogEventProperty>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead)
                continue;

            object? propValue;
            try
            {
                propValue = prop.GetValue(value);
            }
            catch
            {
                continue;
            }

            var sensitiveAttr = prop.GetCustomAttribute<SensitiveDataAttribute>();

            LogEventPropertyValue propLogValue = sensitiveAttr != null
                ? MaskPropertyValue(propValue, prop.PropertyType, sensitiveAttr)
                : DestructureValue(propValue, factory, visited);

            properties.Add(new LogEventProperty(prop.Name, propLogValue));
        }

        return new StructureValue(properties, type.Name);
    }

    private LogEventPropertyValue MaskPropertyValue(
        object? propValue,
        Type propertyType,
        SensitiveDataAttribute attribute)
    {
        if (propValue is null)
            return new ScalarValue(null);

        if (propertyType == typeof(string))
        {
            string? masked = MaskingHelper.MaskByAttribute(propValue.ToString(), attribute);
            return new ScalarValue(masked);
        }

        if (TryHandleMaskedDictionary(propValue, attribute, out var dictionaryResult))
            return dictionaryResult;

        if (TryHandleMaskedEnumerable(propValue, propertyType, attribute, out var sequenceResult))
            return sequenceResult;

        // string olmayan, ama attribute eklenmiş diğer tiplerde fallback
        string? fallback = MaskingHelper.MaskByAttribute(propValue.ToString(), attribute);
        return new ScalarValue(fallback);
    }

    private bool TryHandleDictionary(
        object value,
        ILogEventPropertyValueFactory factory,
        HashSet<object> visited,
        out LogEventPropertyValue result)
    {
        result = null!;

        if (value is not IDictionary dictionary)
            return false;

        var elements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>();

        foreach (DictionaryEntry entry in dictionary)
        {
            var key = new ScalarValue(entry.Key.ToString());
            var val = DestructureValue(entry.Value, factory, visited);
            elements.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(key, val));
        }

        result = new DictionaryValue(elements);
        return true;
    }

    private static bool TryHandleMaskedDictionary(
        object value,
        SensitiveDataAttribute attribute,
        out LogEventPropertyValue result)
    {
        result = null!;

        if (value is not IDictionary dictionary)
            return false;

        var elements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>();

        foreach (DictionaryEntry entry in dictionary)
        {
            string? key = entry.Key.ToString();
            string? raw = entry.Value?.ToString();
            string? masked = MaskingHelper.MaskByAttribute(raw, attribute);

            elements.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                new ScalarValue(key),
                new ScalarValue(masked)));
        }

        result = new DictionaryValue(elements);
        return true;
    }

    private bool TryHandleEnumerable(
        object value,
        ILogEventPropertyValueFactory factory,
        HashSet<object> visited,
        out LogEventPropertyValue result)
    {
        result = null!;

        if (value is string)
            return false;

        if (value is not IEnumerable enumerable)
            return false;

        var items = new List<LogEventPropertyValue>();

        foreach (object? item in enumerable)
        {
            items.Add(DestructureValue(item, factory, visited));
        }

        result = new SequenceValue(items);
        return true;
    }

    private static bool TryHandleMaskedEnumerable(
        object value,
        Type propertyType,
        SensitiveDataAttribute attribute,
        out LogEventPropertyValue result)
    {
        result = null!;

        if (value is string)
            return false;

        if (value is not IEnumerable enumerable)
            return false;

        Type? elementType = GetEnumerableElementType(propertyType);
        if (elementType != typeof(string))
            return false;

        var items = new List<LogEventPropertyValue>();

        foreach (object? item in enumerable)
        {
            string? masked = MaskingHelper.MaskByAttribute(item?.ToString(), attribute);
            items.Add(new ScalarValue(masked));
        }

        result = new SequenceValue(items);
        return true;
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

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(Guid)
               || type == typeof(TimeSpan);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}