using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace HsnSoft.Base.Http;

public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// What every call of a service to another has: the address, the correlation id of the chain (<see cref="CorrelationIdHandler"/>), a total timeout and the standard resilience of .NET (a timeout for each attempt, retries with jitter for what is
    /// worth trying again, a circuit breaker). Reads <see cref="BaseHttpClientOptions"/> from <paramref name="section"/> (a missing section is the defaults, and then the host sets <c>BaseAddress</c> itself). Writes are not retried unless
    /// <see cref="BaseHttpClientOptions.RetryUnsafeMethods"/> says so.
    /// </summary>
    /// <param name="builder">The client, <c>services.AddHttpClient&lt;TClient&gt;()</c>.</param>
    /// <param name="configuration">The configuration that holds the section.</param>
    /// <param name="section">The section of this client (<c>Gateway</c>).</param>
    /// <param name="configure">Changes what the section gave, in code (a client whose POSTs are all reads sets <see cref="BaseHttpClientOptions.RetryUnsafeMethods"/>).</param>
    public static IHttpClientBuilder AddBaseHttpClientDefaults(this IHttpClientBuilder builder, IConfiguration configuration, string section, Action<BaseHttpClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        BaseHttpClientOptions options = configuration.GetSection(section).Get<BaseHttpClientOptions>() ?? new BaseHttpClientOptions();
        configure?.Invoke(options);
        options.Validate(section);

        builder.ConfigureHttpClient(client =>
        {
            if (options.BaseUrl is not null)
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            // the standard handler below limits the call; the client's own limit is only the last word
            client.Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds + 5);
        });
        builder.AddHttpMessageHandler(() => new CorrelationIdHandler(options.CorrelationIdHeader));
        builder.AddStandardResilienceHandler(resilience =>
        {
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds);
            // the strategy needs at least one attempt: "no retries" is a strategy that handles nothing
            resilience.Retry.MaxRetryAttempts = Math.Max(options.MaxRetries, 1);
            if (options.MaxRetries == 0)
            {
                resilience.Retry.ShouldHandle = _ => ValueTask.FromResult(false);
            }

            resilience.Retry.Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds);
            if (!options.RetryUnsafeMethods)
            {
                resilience.Retry.DisableForUnsafeHttpMethods();
            }

            // the window of the breaker has to cover at least two attempts
            TimeSpan twoAttempts = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds * 2);
            if (resilience.CircuitBreaker.SamplingDuration < twoAttempts)
            {
                resilience.CircuitBreaker.SamplingDuration = twoAttempts;
            }

            if (!options.CircuitBreakerEnabled)
            {
                resilience.CircuitBreaker.FailureRatio = 1.0;
                resilience.CircuitBreaker.MinimumThroughput = int.MaxValue;
            }
        });
        return builder;
    }

    /// <summary>
    /// Sends the bearer token of the user being served with the call, and only to <paramref name="allowedHosts"/> (<see cref="BearerTokenRelayHandler"/>). The token is whatever <paramref name="token"/> gives: a web host reads the cookie of the
    /// user, a service the header of its request. Automatic redirects are off for this client, so a redirect cannot take the call (and the token) to another host.
    /// </summary>
    /// <param name="builder">The client.</param>
    /// <param name="token">Gives the token of the user being served (it gets the services of the call), or nothing.</param>
    /// <param name="allowedHosts">The hosts that may get the token, at least one (<c>new Uri(baseUrl).Host</c>).</param>
    public static IHttpClientBuilder AddBearerTokenRelay(this IHttpClientBuilder builder, Func<IServiceProvider, string?> token, params string[] allowedHosts)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(allowedHosts);

        List<string> hosts = [.. allowedHosts];
        builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(2) });
        builder.AddHttpMessageHandler(provider => new BearerTokenRelayHandler(() => token(provider), hosts));
        return builder;
    }

    /// <summary>
    /// For a client that calls an address that came from outside (<see cref="SsrfGuardOptions"/>): the scheme, the port and the host are checked before the call and the address the name resolves to where the connection is made, so only the
    /// public internet is reached, and automatic redirects are off (a redirect would be a new address nobody checked). Do not use it with <see cref="AddBearerTokenRelay"/>: a client for addresses from outside never carries a user's token.
    /// </summary>
    public static IHttpClientBuilder AddSsrfGuard(this IHttpClientBuilder builder, Action<SsrfGuardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        SsrfGuardOptions options = new();
        configure?.Invoke(options);
        builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectCallback = (context, cancellationToken) => SsrfGuard.ConnectAsync(options, context, cancellationToken),
        });
        builder.AddHttpMessageHandler(() => new SsrfGuardHandler(options));
        return builder;
    }
}
