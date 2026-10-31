using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Localization;

/// <summary>The default <see cref="IMessageLocalizer"/>: the text of a key in the current language, with the arguments put in. A key that no language has is logged once (a Warning).</summary>
public sealed partial class MessageLocalizer(LocalizationStore store, ILanguageAccessor language, ILogger<MessageLocalizer> logger) : IMessageLocalizer
{
    /// <summary>How many missing keys are remembered, so each is logged once: a key made of what a client sent would otherwise make an entry for every value, for as long as the process lives.</summary>
    internal const int MaxReportedKeys = 1_000;

    private readonly ConcurrentDictionary<string, bool> _reported = new(StringComparer.Ordinal);
    private int _capReported;

    public string? Find(string key, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(key);

        string? text = store.Find(language.Current, key);
        if (text is null)
        {
            Report(key);
            return null;
        }

        return Format(text, args);
    }

    private void Report(string key)
    {
        if (_reported.Count >= MaxReportedKeys)
        {
            // enough to fix what is missing; the ones after these are not remembered (nor logged), and that is said once
            if (Interlocked.Exchange(ref _capReported, 1) == 0)
            {
                LogReportCapReached(logger, MaxReportedKeys);
            }

            return;
        }

        if (_reported.TryAdd(key, true))
        {
            LogMissing(logger, key);
        }
    }

    public string? FindIfPresent(string key, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(key);

        string? text = store.Find(language.Current, key);
        return text is null ? null : Format(text, args);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The localization key {LocalizationKey} has no text in any language")]
    private static partial void LogMissing(ILogger logger, string localizationKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Count} localization keys without a text were logged; the ones after them are not (a key that is made of what a client sent?)")]
    private static partial void LogReportCapReached(ILogger logger, int count);

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
