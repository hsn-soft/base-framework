using System;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

/// <summary>
/// The lowest level a log must have to reach the persistent sink (Graylog or the file), from <see cref="PersistentLoggerOptions"/>: <c>LogLevel</c> for
/// every category, <c>LevelOverrides</c> for the categories that name, <c>PlatformMinimumLevel</c> for the platform's logs that no override names, and never below
/// <see cref="LogCategories.PersistentFloor"/> (Debug and Trace stay in the console). Built once; every provider asks it the same way.
/// </summary>
public sealed class PersistentLevelRules(PersistentLoggerOptions options)
{
    private readonly LogLevelRules _rules = new(options.LogLevel, options.LevelOverrides);
    private readonly LogLevel _platformMinimum = options.PlatformMinimumLevel;

    /// <param name="category">The category the log was written under.</param>
    /// <param name="isPlatform">Whether it is a <c>PlatformLog</c> (see <see cref="LogTypes"/>).</param>
    public LogLevel For(string category, bool isPlatform)
    {
        ArgumentNullException.ThrowIfNull(category);

        bool named = _rules.TryFor(category, out LogLevel level);
        if (isPlatform && !named)
        {
            level = (LogLevel)Math.Max((int)level, (int)_platformMinimum);
        }

        return (LogLevel)Math.Max((int)level, (int)LogCategories.PersistentFloor);
    }
}
