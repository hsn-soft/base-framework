using System;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>The headers that make a browser safer with the answers of this host.</summary>
    public BaseSecurityHeadersOptions SecurityHeaders { get; set; } = new();

    /// <summary>The browsers of other origins that may call this host.</summary>
    public BaseCorsOptions Cors { get; set; } = new();
}

/// <summary>
/// The security headers of every answer (section <c>Hosting:SecurityHeaders</c>), written by <see cref="BaseHostingExtensions.UseBaseSecurityHeaders"/>: what a browser does with the answer (not guess its type, not show it in a frame, not send the address
/// of the page along, use https). A header the answer already has is left as it is. An empty text leaves a header out.
/// </summary>
public sealed class BaseSecurityHeadersOptions
{
    /// <summary>Whether the headers are written. Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary><c>X-Frame-Options</c>: <c>DENY</c> (default) or <c>SAMEORIGIN</c>; empty for none (a page that is meant to be framed).</summary>
    public string FrameOptions { get; set; } = "DENY";

    /// <summary><c>Referrer-Policy</c>. Default: <c>no-referrer</c>; empty for none.</summary>
    public string ReferrerPolicy { get; set; } = "no-referrer";

    /// <summary>
    /// <c>Strict-Transport-Security</c>: how long a browser must use https only, in seconds (default 31 536 000, one year); 0 for no header. It is only written on an https request and outside Development, so a machine that is not served
    /// over https is never locked out.
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31_536_000;

    /// <summary>Whether the subdomains are included in the HSTS promise. Default: false.</summary>
    public bool HstsIncludeSubDomains { get; set; }

    /// <summary><c>Content-Security-Policy</c>: none by default (a web api needs none); a host with pages sets what its pages load (<c>default-src 'self'</c>).</summary>
    public string ContentSecurityPolicy { get; set; } = string.Empty;

    /// <summary><c>Permissions-Policy</c>: none by default (<c>camera=(), microphone=()</c>).</summary>
    public string PermissionsPolicy { get; set; } = string.Empty;
}

/// <summary>
/// Which browsers of other origins may call this host (section <c>Hosting:Cors</c>). With no origin listed there is no cross-origin policy at all (the default), so no other origin may. The origins are named: a wildcard cannot be used with
/// credentials, and the host does not start with such a setting.
/// </summary>
public sealed class BaseCorsOptions
{
    /// <summary>The name of the policy.</summary>
    public const string PolicyName = "HsnSoftBaseCors";

    /// <summary>The origins that may call (<c>https://backoffice.example.com</c>, no path); <c>*</c> for any, which cannot go with <see cref="AllowCredentials"/>.</summary>
    public List<string> AllowedOrigins { get; set; } = [];

    /// <summary>The methods that may be used. Default: GET, POST, PUT, PATCH, DELETE.</summary>
    public List<string> AllowedMethods { get; set; } = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    /// <summary>The request headers that may be sent. Default: Authorization, Content-Type, X-Correlation-Id, Accept-Language.</summary>
    public List<string> AllowedHeaders { get; set; } = ["Authorization", "Content-Type", "X-Correlation-Id", "Accept-Language"];

    /// <summary>The answer headers the page may read. Default: X-Correlation-Id, Content-Disposition.</summary>
    public List<string> ExposedHeaders { get; set; } = ["X-Correlation-Id", "Content-Disposition"];

    /// <summary>Whether cookies and the authorization of the browser are sent. Default: false.</summary>
    public bool AllowCredentials { get; set; }

    /// <summary>How long a browser keeps the answer to its preflight, in seconds. Default: 600.</summary>
    public int PreflightMaxAgeSeconds { get; set; } = 600;

    /// <summary>The origins in the form the policy compares (no trailing slash); a setting that cannot be right is an <see cref="InvalidOperationException"/>, at the start.</summary>
    public IReadOnlyList<string> NormalizedOrigins()
    {
        List<string> origins = [];
        foreach (string origin in AllowedOrigins.Where(origin => !string.IsNullOrWhiteSpace(origin)).Select(origin => origin.Trim()))
        {
            if (origin == "*")
            {
                if (AllowCredentials)
                {
                    throw new InvalidOperationException("Hosting:Cors: '*' cannot be used with AllowCredentials; name the origins.");
                }

                origins.Add(origin);
                continue;
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https") || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0)
            {
                throw new InvalidOperationException($"Hosting:Cors: '{origin}' is not an origin (scheme, host and port, no path), for example https://backoffice.example.com.");
            }

            origins.Add(uri.GetLeftPart(UriPartial.Authority));
        }

        if (PreflightMaxAgeSeconds is < 0 or > 86_400)
        {
            throw new InvalidOperationException("Hosting:Cors: PreflightMaxAgeSeconds must be between 0 and 86400.");
        }

        return origins;
    }
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
