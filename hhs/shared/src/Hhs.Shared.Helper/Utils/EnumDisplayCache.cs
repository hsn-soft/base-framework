using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Hhs.Shared.Helper.Utils;

public static class EnumDisplayCache<TEnum> where TEnum : struct, Enum
{
    private static readonly ConcurrentDictionary<TEnum, string> s_enumToPrompt = new();
    private static readonly ConcurrentDictionary<string, TEnum> s_promptToEnum = new(StringComparer.OrdinalIgnoreCase);

    static EnumDisplayCache()
    {
        foreach (var field in typeof(TEnum)
                     .GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var enumValue = (TEnum)field.GetValue(null)!;
            var attr = field.GetCustomAttribute<DisplayAttribute>();

            if (attr?.Prompt is null)
            {
                continue;
            }

            s_enumToPrompt[enumValue] = attr.Prompt;
            s_promptToEnum[attr.Prompt] = enumValue;
        }
    }

    // Enum -> Prompt
    public static string ToPrompt(TEnum value)
    {
        return s_enumToPrompt.TryGetValue(value, out string prompt)
            ? prompt
            : value.ToString();
    }

    // Prompt -> Enum
    public static bool TryFromPrompt(string prompt, out TEnum value)
    {
        return s_promptToEnum.TryGetValue(prompt, out value);
    }

    public static TEnum FromPrompt(string prompt) => TryFromPrompt(prompt, out var value)
        ? value
        : throw new ArgumentException($"{typeof(TEnum).Name} enum value not found for '{prompt}'.");
}