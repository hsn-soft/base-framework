using System;
using System.Collections.Generic;

namespace HsnSoft.Base.AspNetCore;

/// <summary>The settings of the health endpoints (section <c>Health</c>).</summary>
public sealed class BaseHealthOptions
{
    public const string SectionName = "Health";

    /// <summary>How long one check may take; a check that takes longer is unhealthy and its token is cancelled. A check registered with a timeout of its own keeps it. Default: 5 seconds.</summary>
    public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The names of the checks this service does not run (<c>redis</c>, <c>rabbitmq</c>, <c>kafka</c>, <c>mongo</c>, <c>minio</c>, the name of a database, ...): a service that has a package but does not
    /// want it to decide when the service is ready, or an environment where a dependency is not there. It is a setting, so it needs no change of code: <c>Health:Disabled:0 = redis</c>.
    /// </summary>
    public List<string> Disabled { get; set; } = [];

    /// <summary>
    /// Whether the answer of a failed check carries the message of its exception. Off by default: the message of a connection error has the address of the server, and the endpoints may be reachable
    /// from further than the platform that probes them. The description of a check never has it.
    /// </summary>
    public bool IncludeErrors { get; set; }
}
