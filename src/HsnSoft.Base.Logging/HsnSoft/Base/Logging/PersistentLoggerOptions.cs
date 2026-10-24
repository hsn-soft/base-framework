using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

/// <summary>
/// What every logging provider reads, so moving from one provider to another does not change the configuration. Two sets of settings, one per place a log goes to:
/// the console is told by the standard <c>Logging:LogLevel</c> (<see cref="ConsoleLevels"/>: a default and a level per category, like
/// <c>"Microsoft.AspNetCore": "Warning"</c>), and what is kept (Graylog or the file) by this section, <c>PersistentLogger</c>. A provider decides how to do what the settings ask.
/// </summary>
public sealed class PersistentLoggerOptions
{
    public const string SectionName = "PersistentLogger";

    /// <summary>
    /// What the console shows: the <c>Logging:LogLevel</c> section (<c>Default</c> and a level per category), as every .NET service has it. Not part of the <c>PersistentLogger</c>
    /// section: it is read from <c>Logging</c>.
    /// </summary>
    public LogLevelRules ConsoleLevels { get; set; } = new(LogLevel.Information);

    /// <summary>
    /// The lowest level of the logs that are kept (Graylog or the file), for every category that <see cref="LevelOverrides"/> does not name. It never goes below
    /// <see cref="LogCategories.PersistentFloor"/> (Information): Debug and Trace stay in the console. Default Information.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogCategories.PersistentFloor;

    /// <summary>
    /// The lowest level of the logs of the platform (ASP.NET Core, Entity Framework, YARP, the drivers: <c>PlatformLog</c>) that reach the persistent sink; they
    /// all stay in the console (what they show there is told by <c>Logging:LogLevel</c>). Default Warning: what the platform says at Information is noise next to the logs of the request and the message.
    /// A category named in <see cref="LevelOverrides"/> is not held to it. Never below <see cref="LogLevel"/>.
    /// </summary>
    public LogLevel PlatformMinimumLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// The start of the namespaces of the application (<c>Hhs</c>): the logs written by the classes under it are <c>AppLog</c>s, <c>HsnSoft.Base.*</c> is the framework's and everything
    /// else (ASP.NET Core, Entity Framework, a driver, any library) is the platform's. When it is not given it is the first part of the name of the assembly
    /// that runs (<c>Hhs.IdentityService.Http.Host</c> is <c>Hhs</c>), which is right where the code of a product has one namespace.
    /// </summary>
    public string? AppNamespace { get; set; }

    /// <summary>Send persistent logs to Graylog; when it is off they go to <see cref="FilePath"/>.</summary>
    public bool IsGrayLogActive { get; set; }

    public GrayLogOptions GrayLog { get; set; } = new();

    public string FilePath { get; set; } = "Logs/logs.txt";

    /// <summary>
    /// The longest text a field of the persistent sink may hold (longer text is cut and the cut is marked; it is above the limits the logs
    /// have for their own fields, a body or a reason of 8 KB, so those are not cut twice), and the room an object logged
    /// with <c>{@Object}</c> gets as the JSON of one field.
    /// </summary>
    public int MaxPersistentFieldLength { get; set; } = 16 * 1024;

    /// <summary>
    /// The version of the running build, written on every log (<c>Version</c>) so a line number in a failure is read in the code that ran. When it
    /// is not given: the environment variable <see cref="VersionVariable"/> (the CI puts the image tag there), else the version of the entry assembly.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>The environment variable that holds the version of the build.</summary>
    public string VersionVariable { get; set; } = "BUILD_NUMBER";

    /// <summary>The version to write on the logs: the setting, the environment variable, the assembly, in that order; <see langword="null"/> when there is none.</summary>
    public string? ResolveVersion(Func<string, string?>? environment = null, Assembly? entry = null)
    {
        if (!string.IsNullOrWhiteSpace(Version))
        {
            return Version;
        }

        string? fromEnvironment = (environment ?? Environment.GetEnvironmentVariable)(VersionVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        string? assembly = (entry ?? Assembly.GetEntryAssembly())?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(assembly) ? null : assembly;
    }

    /// <summary>Show errors of the logging library itself (a sink that cannot reach its server) in the console.</summary>
    public bool IsSelfLogActive { get; set; }

    /// <summary>Per category prefix, the lowest level that is kept for it (e.g. <c>Microsoft.AspNetCore</c> = Error), instead of <see cref="LogLevel"/>; it counts below that prefix too. The console has its own, <c>Logging:LogLevel</c>.</summary>
    public Dictionary<string, LogLevel> LevelOverrides { get; set; } = [];

    /// <summary>
    /// Reads the <c>PersistentLogger</c> section. It is forgiving, as the services in production rely on: level names may be Serilog's
    /// (<c>Verbose</c> is Trace, <c>Fatal</c> is Critical) in any case, and a missing or unreadable value keeps its default.
    /// </summary>
    public static PersistentLoggerOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection(SectionName);
        PersistentLoggerOptions options = new()
        {
            ConsoleLevels = LogLevelRules.FromConfiguration(configuration.GetSection("Logging:LogLevel"), LogLevel.Information),
            LogLevel = ParseLevel(section["LogLevel"], LogCategories.PersistentFloor),
            IsGrayLogActive = bool.TryParse(section["IsGrayLogActive"], out bool grayLog) && grayLog,
            IsSelfLogActive = bool.TryParse(section["IsSelfLogActive"], out bool selfLog) && selfLog,
            Version = section["Version"],
            VersionVariable = string.IsNullOrWhiteSpace(section["VersionVariable"]) ? "BUILD_NUMBER" : section["VersionVariable"]!,
            FilePath = string.IsNullOrWhiteSpace(section["FilePath"]) ? "Logs/logs.txt" : section["FilePath"]!,
            PlatformMinimumLevel = ParseLevel(section["PlatformMinimumLevel"], LogLevel.Warning),
            AppNamespace = string.IsNullOrWhiteSpace(section["AppNamespace"]) ? null : section["AppNamespace"],
            MaxPersistentFieldLength = int.TryParse(section["MaxPersistentFieldLength"], CultureInfo.InvariantCulture, out int maxLength) && maxLength > 0 ? maxLength : 16 * 1024,
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

    internal static bool TryParseLevel(string? text, out LogLevel level)
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
}

public sealed class GrayLogOptions
{
    public string? Address { get; set; }

    public int Port { get; set; } = 12201;

    /// <summary>How often the Graylog sink is rebuilt: a third-party sink can stay broken after a failed start, rebuilding it heals that.</summary>
    public int SinkRefreshIntervalMinutes { get; set; } = 5;
}
