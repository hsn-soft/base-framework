using System;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// How the contexts of a service wait for their database (section <c>EntityFramework</c>). The three providers retry a command that fails in a way that may pass (a connection that broke, a failover), and the defaults of EF Core
/// (six retries, up to thirty seconds between them) keep a request of a pod waiting for about a minute on a database that is down, which under load fills the threads and the connections; these bound it.
/// </summary>
public sealed class EfCoreOptions
{
    public const string SectionName = "EntityFramework";

    /// <summary>How many times a command that failed in a way that may pass is tried again. Default 3; 0 turns the retries off.</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>The longest wait between two tries. Default 5 seconds.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How long one command may run before it is ended. Default 30 seconds (the one of the providers); a service with a long report sets a longer one for that context.</summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether the commands that run are logged (the log of EF Core of category <c>Microsoft.EntityFrameworkCore.Database.Command</c>, at Information: the text of the command and how long it took). Off by default: a busy service writes a line
    /// for every query. A command that <b>fails</b> is logged whatever this says. The values of the parameters are never in the log (they are data).
    /// </summary>
    public bool LogCommands { get; set; }

    /// <summary>The command timeout in whole seconds, as the providers take it.</summary>
    public int CommandTimeoutSeconds => (int)Math.Ceiling(CommandTimeout.TotalSeconds);
}
