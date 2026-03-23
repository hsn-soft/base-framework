using System;
using System.Collections.Generic;
using System.Text;

namespace HsnSoft.Base.Serilog.Mask;

public static class MaskingHelper
{
    private const int DefaultMaskSize = 5;

    private static readonly HashSet<string> s_sensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "pass",
        "username",
        "clientsecret",
        "secret",
        "token",
        "accesstoken",
        "access_token",
        "refreshtoken",
        "refresh_token",
        "authorization",
        "apikey",
        "api_key"
    };

    private static bool IsSensitiveKey(string? key) => !string.IsNullOrWhiteSpace(key) && s_sensitiveKeys.Contains(key);

    public static string? MaskByAttribute(string? currentPropertyValue, SensitiveDataAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(currentPropertyValue))
            return currentPropertyValue;

        if (!string.IsNullOrWhiteSpace(attribute.SubstituteText))
            return attribute.SubstituteText;

        int propertySize = currentPropertyValue.Length;

        if (!AreFirstAndLastParametersInValidRange(propertySize, attribute))
            return new string(GetMaskChar(attribute), DefaultMaskSize);

        int visibleCount = attribute.ShowFirst + attribute.ShowLast;
        int maskSize = attribute.PreserveLength
            ? Math.Max(0, propertySize - visibleCount)
            : DefaultMaskSize;

        var builder = new StringBuilder();

        if (attribute.ShowFirst > 0)
            builder.Append(currentPropertyValue[..attribute.ShowFirst]);

        builder.Append(new string(GetMaskChar(attribute), maskSize));

        if (attribute.ShowLast > 0)
            builder.Append(currentPropertyValue.Substring(propertySize - attribute.ShowLast, attribute.ShowLast));

        return builder.ToString();
    }

    public static string? MaskByKeyName(string? currentValue, string? keyName)
    {
        if (string.IsNullOrWhiteSpace(currentValue))
            return currentValue;

        if (!IsSensitiveKey(keyName))
            return currentValue;

        return new string('*', DefaultMaskSize);
    }

    private static bool AreFirstAndLastParametersInValidRange(int propertySize, SensitiveDataAttribute attribute)
    {
        return attribute.ShowFirst >= 0
               && attribute.ShowLast >= 0
               && attribute.ShowFirst <= propertySize
               && attribute.ShowLast <= propertySize
               && attribute.ShowFirst + attribute.ShowLast <= propertySize;
    }

    private static char GetMaskChar(SensitiveDataAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.Mask))
            return MaskStrings.Default[0];

        return attribute.Mask[0];
    }
}