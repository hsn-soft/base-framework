using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Reflection;

public static class EnumHelper
{
    private static IStringLocalizer _localizer;

    public static void Configure(IStringLocalizerFactory localizerFactory, Type resourceType)
        => _localizer = localizerFactory.Create(resourceType);

    public static IList<T> GetValues<T>() where T : struct, Enum
        => typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(fi => (T)Enum.Parse(typeof(T), fi.Name, false))
            .ToList();

    public static T Parse<T>(string value) where T : struct, Enum
        => (T)Enum.Parse(typeof(T), value, true);

    public static IList<string> GetNames<T>() where T : struct, Enum
        => typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public).Select(fi => fi.Name).ToList();

    public static IList<string> GetDisplayValues<T>() where T : struct, Enum
        => GetNames<T>().Select(obj => GetDisplayValue(Parse<T>(obj))).ToList();

    public static string GetDisplayValue<T>(T value) where T : struct, Enum
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null) return value.ToString();

        var displayAttributes = (DisplayAttribute[])fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false);

        if (displayAttributes is not { Length: > 0 } || string.IsNullOrWhiteSpace(displayAttributes[0].Name)) return value.ToString();

        return displayAttributes[0].ResourceType != null
            ? LookupResource(displayAttributes[0].ResourceType, displayAttributes[0].Name)
            : LookupLocalizer<T>(displayAttributes[0].Name);
    }

    private static string LookupResource(Type resourceManagerProvider, string resourceKey)
    {
        var resourceKeyProperty = resourceManagerProvider.GetProperty(resourceKey,
            BindingFlags.Static | BindingFlags.Public, null, typeof(string),
            Type.EmptyTypes, null);
        if (resourceKeyProperty != null)
        {
            return (string)resourceKeyProperty.GetMethod?.Invoke(null, null);
        }

        return resourceKey;
    }

    private static string LookupLocalizer<T>(string resourceValue) where T : struct, Enum
    {
        if (_localizer == null) return resourceValue ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resourceValue)) return string.Empty;

        var resourceKey = $"Enum:{typeof(T).Name}:{resourceValue}";
        string desc = _localizer[resourceKey];
        if (string.IsNullOrWhiteSpace(desc) || desc.Equals(resourceKey)) desc = _localizer[$"Enum:{resourceValue}"];

        return string.IsNullOrWhiteSpace(desc) || desc.Equals(resourceKey) ? resourceValue : desc;
    }
}