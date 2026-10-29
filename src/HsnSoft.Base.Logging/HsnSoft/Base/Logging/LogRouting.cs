using System;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

/// <summary>Which place a log goes to, as pure functions of its level and category, so the console and the persistent sink are told the same way whichever logging library writes them.</summary>
public static class LogRouting
{
    /// <summary>Logs written under <see cref="LogCategories.ConsoleOnlyPrefix"/> (the console logger) are never persistent.</summary>
    public static bool IsConsoleOnly(string category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return category.StartsWith(LogCategories.ConsoleOnlyPrefix, StringComparison.Ordinal);
    }

    /// <summary>The console shows a log when its level reaches what <c>Logging:LogLevel</c> says for its category (nothing at all for <c>None</c>).</summary>
    public static bool ForConsole(LogLevel level, string category, LogLevelRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        return level >= rules.For(category);
    }

    /// <summary>
    /// The persistent sink (Graylog, file) gets a log when its level reaches what <see cref="PersistentLevelRules"/> says for its category (never below Information, so Debug and Trace logs stay in the console whatever the
    /// configuration says) and it is not for the console only.
    /// </summary>
    /// <param name="level">The level of the log.</param>
    /// <param name="category">The category it was written under.</param>
    /// <param name="logType">Its <see cref="LogTypes"/> (see <see cref="LogShape.LogType"/>).</param>
    /// <param name="rules">The levels of the persistent sink.</param>
    public static bool ForPersistent(LogLevel level, string category, string logType, PersistentLevelRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        return !IsConsoleOnly(category) && level >= rules.For(category, logType == LogTypes.PlatformLog);
    }
}
