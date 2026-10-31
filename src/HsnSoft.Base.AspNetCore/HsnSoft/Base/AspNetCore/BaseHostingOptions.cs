using System;
using System.Collections.Generic;

namespace HsnSoft.Base.AspNetCore;

/// <summary>The settings of the host (section <c>Hosting</c>): what Kestrel accepts, which proxies it trusts and how long it takes to stop. Every value is seen in the settings of the service; none is hidden in code.</summary>
public sealed class BaseHostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>How long a stopping host waits for its hosted services (the consumers of the event bus finish the messages they handle). The terminationGracePeriodSeconds of the pod has to cover it. Default: 30 seconds.</summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    /// <summary>The thread pool of the process.</summary>
    public BaseThreadPoolOptions ThreadPool { get; set; } = new();

    /// <summary>What Kestrel accepts.</summary>
    public BaseKestrelOptions Kestrel { get; set; } = new();

    /// <summary>The proxies a request comes through.</summary>
    public BaseForwardedHeadersOptions ForwardedHeaders { get; set; } = new();
}

/// <summary>The limits of Kestrel (section <c>Hosting:Kestrel</c>). A value that is not set keeps the default of Kestrel (shown in the comment).</summary>
public sealed class BaseKestrelOptions
{
    /// <summary>The largest body of a request, in bytes (Kestrel: 30 000 000, about 28.6 MB). <c>0</c> or less: no limit.</summary>
    public long? MaxRequestBodySize { get; set; }

    /// <summary>How much of a request Kestrel reads ahead of the application, in bytes (Kestrel: 1 048 576).</summary>
    public long? MaxRequestBufferSize { get; set; }

    /// <summary>The largest total size of the headers of a request, in bytes (Kestrel: 32 768).</summary>
    public int? MaxRequestHeadersTotalSize { get; set; }

    /// <summary>The largest number of headers of a request (Kestrel: 100).</summary>
    public int? MaxRequestHeaderCount { get; set; }

    /// <summary>How long Kestrel waits for the headers of a request (Kestrel: 30 seconds).</summary>
    public TimeSpan? RequestHeadersTimeout { get; set; }

    /// <summary>How long an idle connection is kept (Kestrel: 130 seconds).</summary>
    public TimeSpan? KeepAliveTimeout { get; set; }

    /// <summary>The largest number of open connections (Kestrel: no limit).</summary>
    public long? MaxConcurrentConnections { get; set; }
}

/// <summary>
/// The proxies whose <c>X-Forwarded-*</c> headers are believed (section <c>Hosting:ForwardedHeaders</c>). A header from anyone else is ignored, so a client cannot say it is somebody else. When neither list is
/// set only a proxy on the same machine is believed (the default of ASP.NET Core).
/// </summary>
public sealed class BaseForwardedHeadersOptions
{
    /// <summary>Whether the host reads the headers. Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The addresses of the proxies (<c>10.0.0.4</c>).</summary>
    public List<string> KnownProxies { get; set; } = [];

    /// <summary>The networks of the proxies in CIDR form (<c>10.244.0.0/16</c> for the pods of a cluster).</summary>
    public List<string> KnownNetworks { get; set; } = [];

    /// <summary>How many proxies are believed in a chain. Default: 1.</summary>
    public int ForwardLimit { get; set; } = 1;
}

/// <summary>
/// The threads the pool has ready (section <c>Hosting:ThreadPool</c>). The pool starts with as many threads as there are cores and adds one every half second when all are busy; a service that blocks
/// threads (a synchronous call, a lock) or has many consumers waits for those threads while it is busy. A value is only ever <b>raised</b>: a minimum lower than the one in force is ignored.
/// </summary>
public sealed class BaseThreadPoolOptions
{
    /// <summary>The worker threads ready at once; null leaves the default (the number of cores).</summary>
    public int? MinWorkerThreads { get; set; }

    /// <summary>The threads for completion ports (I/O) ready at once; null leaves the default.</summary>
    public int? MinCompletionPortThreads { get; set; }
}
