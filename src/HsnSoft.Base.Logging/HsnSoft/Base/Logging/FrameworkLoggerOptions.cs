using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

/// <summary>
/// What every logging provider reads (section <c>FrameworkLogger</c>), so moving from one provider to another does not change the
/// configuration. A provider decides how to do what the settings ask.
/// </summary>
public sealed class FrameworkLoggerOptions
{
    public const string SectionName = "FrameworkLogger";

    /// <summary>The lowest level written anywhere. Default: everything.</summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Trace;

    /// <summary>The lowest level of the persistent sink. It never goes below <see cref="LogCategories.PersistentFloor"/>: Debug and Trace stay in the console.</summary>
    public LogLevel PersistentMinimumLevel { get; set; } = LogCategories.PersistentFloor;

    /// <summary>Send persistent logs to Graylog; when it is off they go to <see cref="FilePath"/>.</summary>
    public bool IsGrayLogActive { get; set; }

    public GrayLogOptions GrayLog { get; set; } = new();

    public string FilePath { get; set; } = "Logs/logs.txt";

    /// <summary>Show errors of the logging library itself (a sink that cannot reach its server) in the console.</summary>
    public bool IsSelfLogActive { get; set; }

    /// <summary>Per category prefix, the lowest level written for it (e.g. <c>Microsoft.AspNetCore</c> = Warning); added to the defaults of the provider.</summary>
    public Dictionary<string, LogLevel> LevelOverrides { get; set; } = [];

    /// <summary>
    /// Reads the <c>FrameworkLogger</c> section. It is forgiving, as the services in production rely on: level names may be Serilog's
    /// (<c>Verbose</c> is Trace, <c>Fatal</c> is Critical) in any case, and a missing or unreadable value keeps its default.
    /// </summary>
    public static FrameworkLoggerOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection(SectionName);
        FrameworkLoggerOptions options = new()
        {
            LogLevel = ParseLevel(section["LogLevel"], LogLevel.Trace),
            IsGrayLogActive = bool.TryParse(section["IsGrayLogActive"], out bool grayLog) && grayLog,
            IsSelfLogActive = bool.TryParse(section["IsSelfLogActive"], out bool selfLog) && selfLog,
            FilePath = string.IsNullOrWhiteSpace(section["FilePath"]) ? "Logs/logs.txt" : section["FilePath"]!,
            PersistentMinimumLevel = ParseLevel(section["PersistentMinimumLevel"], LogCategories.PersistentFloor)
        };
        options.GrayLog.Address = section["GrayLog:Address"];
        options.GrayLog.Port = int.TryParse(section["GrayLog:Port"], CultureInfo.InvariantCulture, out int port) ? port : options.GrayLog.Port;
        options.GrayLog.SinkRefreshIntervalMinutes = int.TryParse(section["GrayLog:SinkRefreshIntervalMinutes"], CultureInfo.InvariantCulture, out int minutes) && minutes > 0
            ? minutes
            : options.GrayLog.SinkRefreshIntervalMinutes;

        foreach (IConfigurationSection child in section.GetSection("LevelOverrides").GetChildren())
        {
            if (TryParseLevel(child.Value, out LogLevel level))
            {
                options.LevelOverrides[child.Key] = level;
            }
        }

        return options;
    }

    private static LogLevel ParseLevel(string? text, LogLevel fallback) => TryParseLevel(text, out LogLevel level) ? level : fallback;

    private static bool TryParseLevel(string? text, out LogLevel level)
    {
        switch (text?.Trim().ToLowerInvariant())
        {
            case "verbose":
                level = LogLevel.Trace;
                return true;
            case "fatal":
                level = LogLevel.Critical;
                return true;
            default:
                return Enum.TryParse(text, ignoreCase: true, out level) && Enum.IsDefined(level);
        }
    }

    /// <summary>The level a log must have to be persistent: <see cref="PersistentMinimumLevel"/>, at least <see cref="LogLevel"/>, and never below the floor.</summary>
    public LogLevel EffectivePersistentLevel
        => (LogLevel)System.Math.Max((int)PersistentMinimumLevel, System.Math.Max((int)LogLevel, (int)LogCategories.PersistentFloor));
}

public sealed class GrayLogOptions
{
    public string? Address { get; set; }

    public int Port { get; set; } = 12201;

    /// <summary>How often the Graylog sink is rebuilt: a third-party sink can stay broken after a failed start, rebuilding it heals that.</summary>
    public int SinkRefreshIntervalMinutes { get; set; } = 5;
}
