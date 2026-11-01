using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>The host of a service: its settings in one place (<see cref="BaseHostingOptions"/>), the proxies it trusts, the settings it cannot start without and a start that logs why it failed.</summary>
public static class BaseHostingExtensions
{
    /// <summary>
    /// Sets the host up from the settings: the keys of a <c>.env</c> file (<see cref="EnvFile.AddBaseEnvFile"/>), the limits of Kestrel, the proxies it believes and the time it takes to stop (section
    /// <c>Hosting</c>, <see cref="BaseHostingOptions"/>). Call it first, before the settings are read by anything else.
    /// </summary>
    /// <param name="builder">The builder of the host.</param>
    public static WebApplicationBuilder AddBaseHosting(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddBaseEnvFile();
        BaseHostingOptions options = builder.Configuration.GetSection(BaseHostingOptions.SectionName).Get<BaseHostingOptions>() ?? new BaseHostingOptions();

        builder.WebHost.ConfigureKestrel((context, kestrel) =>
        {
            BaseKestrelOptions limits = context.Configuration.GetSection(BaseHostingOptions.SectionName).Get<BaseHostingOptions>()?.Kestrel ?? new BaseKestrelOptions();
            Apply(limits, kestrel.Limits);
        });

        ApplyThreadPool(options.ThreadPool);
        builder.Services.AddSingleton(AppInfo.Create(builder.Environment, builder.Configuration["Service:Name"]));
        builder.Services.AddOptions<BaseHostingOptions>().Bind(builder.Configuration.GetSection(BaseHostingOptions.SectionName));
        builder.Services.Configure<HostOptions>(host => host.ShutdownTimeout = TimeSpan.FromSeconds(options.ShutdownTimeoutSeconds));
        builder.Services.Configure<ForwardedHeadersOptions>(forwarded => Apply(options.ForwardedHeaders, forwarded));
        return builder;
    }

    /// <summary>
    /// Maps <c>GET /</c>: who this instance is (<see cref="AppInfo"/>: the service, the instance, the environment, the version, the start), for anyone who can reach it, a person with a browser or a probe of a platform. It says nothing a
    /// log does not; the address is the root of the service, so a service that has a page there maps its own instead.
    /// </summary>
    public static IEndpointRouteBuilder MapBaseInfo(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/", (AppInfo info) => Results.Json(info, new JsonSerializerOptions(JsonSerializerDefaults.Web))).AllowAnonymous().ExcludeFromDescription();
        return endpoints;
    }

    /// <summary>The minimum threads of the pool, raised to the settings; a value lower than the one in force is left alone. Returns what is in force after it.</summary>
    /// <param name="options">The settings.</param>
    public static (int Worker, int CompletionPort) ApplyThreadPool(BaseThreadPoolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ThreadPool.GetMinThreads(out int worker, out int completion);
        int wantedWorker = Math.Max(worker, options.MinWorkerThreads ?? 0);
        int wantedCompletion = Math.Max(completion, options.MinCompletionPortThreads ?? 0);
        if ((wantedWorker != worker || wantedCompletion != completion) && ThreadPool.SetMinThreads(wantedWorker, wantedCompletion))
        {
            return (wantedWorker, wantedCompletion);
        }

        return (worker, completion);
    }

    /// <summary>
    /// Believes the <c>X-Forwarded-*</c> headers of the proxies of <c>Hosting:ForwardedHeaders</c>: the address, the scheme and the host of the caller are the ones the proxy saw. It is the first middleware, so what comes
    /// after it (the logs, the authentication) sees the real caller. Nothing is done when <c>Hosting:ForwardedHeaders:Enabled</c> is false.
    /// </summary>
    public static WebApplication UseBaseForwardedHeaders(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Services.GetRequiredService<IOptions<BaseHostingOptions>>().Value.ForwardedHeaders.Enabled)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }

    /// <summary>
    /// Settings the service cannot start without (an API key, an address): <see cref="RunBaseAsync(WebApplicationBuilder, Action{WebApplication})"/> stops it before it listens, with one error that names every one that is missing (never a value).
    /// A package or a service adds its own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="keys">The keys, with <c>:</c> between the levels (<c>Scheduler:ApiKey</c>).</param>
    public static IServiceCollection RequireBaseSettings(this IServiceCollection services, params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(keys);

        services.AddSingleton(new RequiredSettings(keys));
        return services;
    }

    /// <summary>
    /// Builds the host, checks the required settings and runs it; a failure at any of these steps is logged as critical (what failed and why, once) and ends the process with the exit code 1, so the platform sees a failed start
    /// and restarts it. Before the host is built there is no logger, so such a failure goes to the standard error.
    /// </summary>
    /// <param name="builder">The builder of the host.</param>
    /// <param name="configure">Adds the middleware and maps the endpoints.</param>
    /// <returns>0 after a normal stop, 1 after a failure.</returns>
    public static Task<int> RunBaseAsync(this WebApplicationBuilder builder, Action<WebApplication> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return builder.RunBaseAsync(app =>
        {
            configure(app);
            return Task.CompletedTask;
        });
    }

    /// <summary>The same, for a host whose configuration awaits something (seeding the data of a mock before it listens).</summary>
    /// <param name="builder">The builder of the host.</param>
    /// <param name="configure">Adds the middleware and maps the endpoints.</param>
    public static async Task<int> RunBaseAsync(this WebApplicationBuilder builder, Func<WebApplication, Task> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        WebApplication? app = null;
        try
        {
            app = builder.Build();
            CheckRequiredSettings(app);
            await configure(app);
            await app.RunAsync();
            return 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (app is null)
            {
                await Console.Error.WriteLineAsync($"The host could not be built: {exception}");
            }
            else
            {
                s_failed(app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HsnSoft.Base.AspNetCore.Startup"), exception);
            }

            return 1;
        }
        finally
        {
            if (app is not null)
            {
                await app.DisposeAsync();   // the logger providers flush what they hold, the failure above among it
            }
        }
    }

    private static void CheckRequiredSettings(WebApplication app)
    {
        IConfiguration configuration = app.Services.GetRequiredService<IConfiguration>();
        string[] missing = [.. app.Services.GetServices<RequiredSettings>().SelectMany(required => required.Keys)
            .Where(key => string.IsNullOrWhiteSpace(configuration[key])).Distinct(StringComparer.OrdinalIgnoreCase)];
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"These settings are required and not set: {string.Join(", ", missing)}. Set them in the settings file, the environment or a .env file.");
        }
    }

    private static void Apply(BaseKestrelOptions options, KestrelServerLimits limits)
    {
        if (options.MaxRequestBodySize is { } body)
        {
            limits.MaxRequestBodySize = body <= 0 ? null : body;
        }

        limits.MaxRequestBufferSize = options.MaxRequestBufferSize ?? limits.MaxRequestBufferSize;
        limits.MaxRequestHeadersTotalSize = options.MaxRequestHeadersTotalSize ?? limits.MaxRequestHeadersTotalSize;
        limits.MaxRequestHeaderCount = options.MaxRequestHeaderCount ?? limits.MaxRequestHeaderCount;
        limits.RequestHeadersTimeout = options.RequestHeadersTimeout ?? limits.RequestHeadersTimeout;
        limits.KeepAliveTimeout = options.KeepAliveTimeout ?? limits.KeepAliveTimeout;
        limits.MaxConcurrentConnections = options.MaxConcurrentConnections ?? limits.MaxConcurrentConnections;
    }

    private static void Apply(BaseForwardedHeadersOptions options, ForwardedHeadersOptions forwarded)
    {
        forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        forwarded.ForwardLimit = options.ForwardLimit;
        foreach (string proxy in options.KnownProxies)
        {
            forwarded.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (string network in options.KnownNetworks)
        {
            forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    }

    private static readonly Action<ILogger, Exception?> s_failed = LoggerMessage.Define(LogLevel.Critical, new EventId(1, "HostFailed"), "The host failed to start or stopped with a failure");

    private sealed record RequiredSettings(string[] Keys);
}
