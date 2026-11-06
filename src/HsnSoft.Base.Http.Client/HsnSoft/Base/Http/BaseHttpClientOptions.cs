using System;

namespace HsnSoft.Base.Http;

/// <summary>
/// The settings of a client that calls another service, read from a section the host names (<c>Gateway</c>, <c>Billing</c>): where it calls, how long it waits and when it tries again. Every value has a default that is safe.
/// </summary>
public sealed class BaseHttpClientOptions
{
    /// <summary>The address the client calls (<c>https://gateway.internal</c>). Required unless the host sets <c>HttpClient.BaseAddress</c> itself.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>The time one attempt may take, in seconds (default 10).</summary>
    public int AttemptTimeoutSeconds { get; set; } = 10;

    /// <summary>The time the call may take with its retries, in seconds (default 30).</summary>
    public int TotalTimeoutSeconds { get; set; } = 30;

    /// <summary>How many times a failed call is tried again (default 3, 0 for none): a timeout, a connection failure, a 5xx answer, a 408 and a 429.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>The wait before the first retry, in milliseconds (default 2000); it grows with each retry and has jitter.</summary>
    public int RetryDelayMilliseconds { get; set; } = 2000;

    /// <summary>
    /// Whether a call that changes something (POST, PUT, PATCH, DELETE) is tried again too (default <see langword="false"/>): a write that failed after the other service did it would be done twice. Turn it on only for a client whose
    /// calls are idempotent (a search that is a POST, a call with an idempotency key).
    /// </summary>
    public bool RetryUnsafeMethods { get; set; }

    /// <summary>Whether the circuit breaker is on (default <see langword="true"/>): after a run of failures calls fail at once for a while, instead of piling up on a service that is down.</summary>
    public bool CircuitBreakerEnabled { get; set; } = true;

    /// <summary>The name of the header that carries the correlation id of the chain (default <c>X-Correlation-Id</c>).</summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";

    internal void Validate(string section)
    {
        if (AttemptTimeoutSeconds is < 1 or > 600)
        {
            throw new InvalidOperationException($"{section}:AttemptTimeoutSeconds must be between 1 and 600 seconds.");
        }

        if (TotalTimeoutSeconds < AttemptTimeoutSeconds || TotalTimeoutSeconds > 3600)
        {
            throw new InvalidOperationException($"{section}:TotalTimeoutSeconds must be at least AttemptTimeoutSeconds and at most 3600 seconds.");
        }

        if (MaxRetries is < 0 or > 10)
        {
            throw new InvalidOperationException($"{section}:MaxRetries must be between 0 and 10.");
        }

        if (RetryDelayMilliseconds is < 0 or > 60_000)
        {
            throw new InvalidOperationException($"{section}:RetryDelayMilliseconds must be between 0 and 60000.");
        }

        if (string.IsNullOrWhiteSpace(CorrelationIdHeader))
        {
            throw new InvalidOperationException($"{section}:CorrelationIdHeader must not be empty.");
        }

        if (BaseUrl is not null)
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? uri))
            {
                throw new InvalidOperationException($"{section}:BaseUrl is not an absolute address.");
            }

            if (uri.Scheme is not ("http" or "https"))
            {
                throw new InvalidOperationException($"{section}:BaseUrl must be an http or https address.");
            }
        }
    }
}
