using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Hhs.Shared.Helper.Utils;

public static class EnumExtensions
{
    public static string ToPrompt<TEnum>(this TEnum value) where TEnum : struct, Enum
        => EnumDisplayCache<TEnum>.ToPrompt(value);

    public static TEnum ToEnumFromPrompt<TEnum>(this string prompt) where TEnum : struct, Enum
        => EnumDisplayCache<TEnum>.FromPrompt(prompt);

    public static bool TryToEnumFromPrompt<TEnum>(this string prompt, out TEnum value) where TEnum : struct, Enum
        => EnumDisplayCache<TEnum>.TryFromPrompt(prompt, out value);

    public static string ToDisplayName(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();

        return attribute?.Name ?? value.ToString();
    }
}

// var prompt = BuildingType.JumpGate.ToPrompt();
// "jump-gate"

// var building = "jump-gate".ToEnumFromPrompt<BuildingType>();
// BuildingType.JumpGate

// if ("jump-gate".TryToEnumFromPrompt<BuildingType>(out var building))
// {
//     // OK
// }