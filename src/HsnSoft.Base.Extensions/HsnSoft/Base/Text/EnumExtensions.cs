using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace HsnSoft.Base.Text;

/// <summary>
/// An enum as a client knows it: <c>[Display(Name = "Video Platform", Prompt = "video-platform")]</c> on a member gives it a <b>name</b> (a description, with spaces, for a person) and a <b>prompt</b> (a key: small letters, words joined by <c>-</c>,
/// what a front end sends to the back end). <see cref="ToPrompt{TEnum}"/> and <see cref="ToEnumFromPrompt{TEnum}"/> go from the member to the key and back; a member with no prompt has the key made of its name (<c>VideoPlatform</c> is <c>video-platform</c>),
/// so every member can be sent and read. What is read from the attributes is kept per enum type, so a call does not use reflection.
/// </summary>
public static class EnumExtensions
{
    /// <summary>The key of the member (its prompt, or its name in small letters joined by <c>-</c>); the text of the number for a value that is not a member.</summary>
    public static string ToPrompt<TEnum>(this TEnum value)
        where TEnum : struct, Enum
        => EnumDisplay<TEnum>.Prompts.TryGetValue(value, out string? prompt) ? prompt : value.ToString();

    /// <summary>The name of the member for a person (the <c>Name</c> of its <c>Display</c> attribute, or the name of the member); the text of the number for a value that is not a member.</summary>
    public static string ToDisplayName<TEnum>(this TEnum value)
        where TEnum : struct, Enum
        => EnumDisplay<TEnum>.Names.TryGetValue(value, out string? name) ? name : value.ToString();

    /// <summary>The member that has this key (compared without regard to case).</summary>
    /// <exception cref="ArgumentException">No member has the key.</exception>
    public static TEnum ToEnumFromPrompt<TEnum>(this string prompt)
        where TEnum : struct, Enum
        => prompt.TryToEnumFromPrompt(out TEnum value) ? value : throw new ArgumentException($"{typeof(TEnum).Name} has no value for '{prompt}'.", nameof(prompt));

    /// <summary>Whether a member has this key (compared without regard to case), and the member.</summary>
    public static bool TryToEnumFromPrompt<TEnum>(this string? prompt, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        return !string.IsNullOrWhiteSpace(prompt) && EnumDisplay<TEnum>.Values.TryGetValue(prompt.Trim(), out value);
    }

    /// <summary>The key a member without a prompt has: <c>VideoPlatform</c> is <c>video-platform</c>, <c>EInvoice</c> is <c>e-invoice</c>.</summary>
    internal static string KeyOf(string memberName)
    {
        StringBuilder key = new(memberName.Length + 4);
        for (int i = 0; i < memberName.Length; i++)
        {
            char c = memberName[i];
            if (char.IsUpper(c) && i > 0 && (char.IsLower(memberName[i - 1]) || (i + 1 < memberName.Length && char.IsLower(memberName[i + 1]))))
            {
                key.Append('-');
            }

            key.Append(char.ToLowerInvariant(c));
        }

        return key.ToString();
    }

    private static class EnumDisplay<TEnum>
        where TEnum : struct, Enum
    {
        public static readonly FrozenDictionary<TEnum, string> Prompts;
        public static readonly FrozenDictionary<TEnum, string> Names;
        public static readonly FrozenDictionary<string, TEnum> Values;

        static EnumDisplay()
        {
            Dictionary<TEnum, string> prompts = [];
            Dictionary<TEnum, string> names = [];
            Dictionary<string, TEnum> values = [with(StringComparer.OrdinalIgnoreCase)];

            foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                TEnum member = (TEnum)field.GetValue(null)!;
                DisplayAttribute? display = field.GetCustomAttribute<DisplayAttribute>();
                string prompt = string.IsNullOrWhiteSpace(display?.Prompt) ? KeyOf(field.Name) : display.Prompt.Trim();

                prompts.TryAdd(member, prompt);
                names.TryAdd(member, string.IsNullOrWhiteSpace(display?.Name) ? field.Name : display.Name.Trim());
                values.TryAdd(prompt, member);
            }

            Prompts = prompts.ToFrozenDictionary();
            Names = names.ToFrozenDictionary();
            Values = values.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }
    }
}
