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

    /// <summary>The command timeout in whole seconds, as the providers take it.</summary>
    public int CommandTimeoutSeconds => (int)Math.Ceiling(CommandTimeout.TotalSeconds);
}
