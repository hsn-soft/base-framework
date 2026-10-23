namespace HsnSoft.Base.Logging;

/// <summary>
/// Logger categories (the name a log is written under) that logging providers recognize. A category is the one thing every provider
/// (Serilog's <c>SourceContext</c>, NLog's logger name) knows, so routing by it works the same everywhere.
/// </summary>
public static class LogCategories
{
    /// <summary>Logs written under a category that starts with this stay out of the persistent sink (Graylog, file).</summary>
    public const string ConsoleOnlyPrefix = "ConsoleOnly.";

    /// <summary>The category of structured logs written with <see cref="Abstracts.IFrameworkLogger"/>; the console shows their properties.</summary>
    public const string FrameworkLog = "FrameworkLog";

    /// <summary>The lowest level that is ever persistent: Debug and Trace (Serilog's Verbose) logs are for the console only.</summary>
    public const Microsoft.Extensions.Logging.LogLevel PersistentFloor = Microsoft.Extensions.Logging.LogLevel.Information;
}
