using System;
using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Localization;

/// <summary>The default <see cref="IMessageLocalizer"/>: the text of a key in the current language, with the arguments put in. A key that no language has is logged once (a Warning).</summary>
public sealed partial class MessageLocalizer(LocalizationStore store, ILanguageAccessor language, ILogger<MessageLocalizer> logger) : IMessageLocalizer
{
    private readonly ConcurrentDictionary<string, bool> _reported = new(StringComparer.Ordinal);

    public string? Find(string key, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(key);

        string? text = store.Find(language.Current, key);
        if (text is null)
        {
            if (_reported.TryAdd(key, true))
            {
                LogMissing(logger, key);
            }

            return null;
        }

        return Format(text, args);
    }

    public string? FindIfPresent(string key, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(key);

        string? text = store.Find(language.Current, key);
        return text is null ? null : Format(text, args);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The localization key {LocalizationKey} has no text in any language")]
    private static partial void LogMissing(ILogger logger, string localizationKey);

    internal static string Format(string text, object?[]? args)
    {
        if (args is null || args.Length == 0)
        {
            return text;
        }

        try
        {
            return string.Format(CultureInfo.InvariantCulture, text, args);
        }
        catch (FormatException)
        {
            // a text with a place that is not given must not fail the answer that carries it
            return text;
        }
    }
}
