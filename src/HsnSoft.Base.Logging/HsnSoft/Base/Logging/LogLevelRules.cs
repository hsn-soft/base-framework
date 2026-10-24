using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

/// <summary>
/// The lowest level written per category, as <c>Microsoft.Extensions.Logging</c> reads it (<c>Logging:LogLevel</c>): a default and rules for categories
/// (<c>"Microsoft.AspNetCore": "Warning"</c>) that also hold for everything below them (<c>Microsoft.AspNetCore.Routing</c>); the longest rule that fits wins.
/// A provider applies it itself (the logging builder's own filter is opened wide, so the console and the persistent sink can have different levels).
/// </summary>
public sealed class LogLevelRules(LogLevel defaultLevel, IEnumerable<KeyValuePair<string, LogLevel>>? categories = null)
{
    private readonly KeyValuePair<string, LogLevel>[] _categories = [.. (categories ?? []).OrderByDescending(category => category.Key.Length)];

    /// <summary>The level of a category that no rule names.</summary>
    public LogLevel Default { get; } = defaultLevel;

    /// <summary>The lowest level written for <paramref name="category"/>.</summary>
    public LogLevel For(string category) => TryFor(category, out LogLevel level) ? level : Default;

    /// <summary>The level a rule gives <paramref name="category"/>; <see langword="false"/> when no rule names it (then <see cref="Default"/> holds).</summary>
    public bool TryFor(string category, out LogLevel level)
    {
        foreach (KeyValuePair<string, LogLevel> rule in _categories)
        {
            // "Microsoft" is the category "Microsoft" and everything below it, not "MicrosoftExtras"
            if (category.StartsWith(rule.Key, StringComparison.OrdinalIgnoreCase) && (category.Length == rule.Key.Length || category[rule.Key.Length] == '.'))
            {
                level = rule.Value;
                return true;
            }
        }

        level = Default;
        return false;
    }

    /// <summary>Reads a section like <c>Logging:LogLevel</c>: <c>Default</c> and a level per category. A missing or unreadable level is left out.</summary>
    public static LogLevelRules FromConfiguration(IConfigurationSection section, LogLevel fallbackDefault)
    {
        ArgumentNullException.ThrowIfNull(section);

        LogLevel defaultLevel = PersistentLoggerOptions.TryParseLevel(section["Default"], out LogLevel parsed) ? parsed : fallbackDefault;
        List<KeyValuePair<string, LogLevel>> categories = [];
        foreach (IConfigurationSection child in section.GetChildren())
        {
            if (!string.Equals(child.Key, "Default", StringComparison.OrdinalIgnoreCase) && PersistentLoggerOptions.TryParseLevel(child.Value, out LogLevel level))
            {
                categories.Add(new KeyValuePair<string, LogLevel>(child.Key, level));
            }
        }

        return new LogLevelRules(defaultLevel, categories);
    }
}
