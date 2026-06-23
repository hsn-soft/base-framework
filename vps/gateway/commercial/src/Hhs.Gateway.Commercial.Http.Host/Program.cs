using HealthChecks.UI.Client;
using Hhs.Gateway.Commercial;
using Hhs.Gateway.Commercial.Middlewares;
using Hhs.Gateway.Commercial.Options;
using Hhs.Gateway.Commercial.Options.Docs;
using Hhs.Gateway.Commercial.Services;
using Hhs.Gateway.Commercial.Services.Docs;
using Hhs.Gateway.Commercial.Services.Swagger;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Gateways.Extensions;
using Hhs.Shared.Hosting.Helpers;
using Hhs.Shared.Hosting.Middlewares;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Swashbuckle;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// App info
AppInfoResolver.Resolve(typeof(EventHandlersAssemblyMarker));

// Config
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Serilog
Log.Logger = SerilogConfigurationHelper.ConfigureConsoleLogger(builder.Configuration, "Host");
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

// Network Security
builder.Services.AddOptions<GatewayPolicyOptions>().Bind(builder.Configuration.GetSection("GatewayPolicies")).ValidateOnStart();
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var policy = context.Configuration.GetSection("GatewayPolicies").Get<GatewayPolicyOptions>() ?? new GatewayPolicyOptions();

    options.Limits.MaxRequestBodySize = policy.Kestrel.MaxRequestBodySizeBytes;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(policy.Kestrel.RequestHeadersTimeoutSeconds);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(policy.Kestrel.KeepAliveTimeoutSeconds);
    options.Limits.MaxRequestHeaderCount = policy.Kestrel.MaxRequestHeaderCount;
    options.Limits.MaxRequestHeadersTotalSize = policy.Kestrel.MaxRequestHeadersTotalSizeBytes;

    if (!builder.Environment.IsDevelopment()) return;

    options.ListenAnyIP(7200);
    options.ListenAnyIP(7201, listenOptions => { listenOptions.UseHttps("../../etc/dev-cert/localhost.pfx", "e8202f07-66e5-4619-be07-72ba76fde97f"); });
});
builder.Services.AddDefaultCorsSettings("default");
// builder.Services.AddCors(options =>
// {
//     var policyOptions = builder.Configuration.GetSection("GatewayPolicies").Get<GatewayPolicyOptions>() ?? new GatewayPolicyOptions();
//
//     options.AddPolicy("GatewayCorsPolicy", cors =>
//     {
//         if (policyOptions.AllowedCorsOrigins.Length > 0)
//         {
//             cors.WithOrigins(policyOptions.AllowedCorsOrigins)
//                 .AllowAnyHeader()
//                 .AllowAnyMethod()
//                 .AllowCredentials();
//         }
//         else
//         {
//             cors.DisallowCredentials()
//                 .AllowAnyHeader()
//                 .AllowAnyMethod();
//         }
//     });
// });
// builder.Services.AddRateLimiter(options =>
// {
//     options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//
//     options.OnRejected = async (context, token) =>
//     {
//         if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
//         {
//             context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
//         }
//
//         await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Rate limit exceeded." }, token);
//     };
//
//     options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
//     {
//         var policy = httpContext.RequestServices.GetRequiredService<IOptions<GatewayPolicyOptions>>().Value;
//         var category = GatewayRouteClassifier.GetRateCategory(httpContext, policy);
//         string requesterKey = GatewayRouteClassifier.GetPartitionKey(httpContext);
//
//         return category switch
//         {
//             GatewayRateCategory.Docs =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"docs:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.SwaggerPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = policy.RateLimits.QueueLimit, AutoReplenishment = true }),
//
//             GatewayRateCategory.IdentityLogin =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"identity-login:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.IdentityLoginPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }),
//
//             GatewayRateCategory.IdentityAuth =>
//                 RateLimitPartition.GetSlidingWindowLimiter(
//                     partitionKey: $"identity-auth:{requesterKey}",
//                     factory: _ => new SlidingWindowRateLimiterOptions
//                     {
//                         PermitLimit = policy.RateLimits.IdentityAuthPerMinute,
//                         Window = TimeSpan.FromMinutes(1),
//                         SegmentsPerWindow = 6,
//                         QueueLimit = policy.RateLimits.QueueLimit,
//                         AutoReplenishment = true
//                     }),
//
//             GatewayRateCategory.ContentSearch =>
//                 RateLimitPartition.GetSlidingWindowLimiter(
//                     partitionKey: $"content-search:{requesterKey}",
//                     factory: _ => new SlidingWindowRateLimiterOptions
//                     {
//                         PermitLimit = policy.RateLimits.ContentSearchPerMinute,
//                         Window = TimeSpan.FromMinutes(1),
//                         SegmentsPerWindow = 6,
//                         QueueLimit = policy.RateLimits.QueueLimit,
//                         AutoReplenishment = true
//                     }),
//
//             GatewayRateCategory.ContentExport =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"content-export:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.ContentExportPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }),
//
//             GatewayRateCategory.ContentDefaultRead =>
//                 RateLimitPartition.GetTokenBucketLimiter(
//                     partitionKey: $"content-read:{requesterKey}",
//                     factory: _ => new TokenBucketRateLimiterOptions
//                     {
//                         TokenLimit = policy.RateLimits.ContentDefaultReadPerMinute,
//                         TokensPerPeriod = policy.RateLimits.ContentDefaultReadPerMinute,
//                         ReplenishmentPeriod = TimeSpan.FromMinutes(1),
//                         QueueLimit = policy.RateLimits.QueueLimit,
//                         AutoReplenishment = true
//                     }),
//
//             GatewayRateCategory.TextNormalizer =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"text-normalizer:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.TextNormalizerPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }),
//
//             GatewayRateCategory.VideoGenerator =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"video-generator:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.VideoGeneratorPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }),
//
//             _ =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"global:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.GlobalPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = policy.RateLimits.QueueLimit, AutoReplenishment = true })
//         };
//     });
// });

builder.Services
    .AddGatewayHosting(builder.Configuration)
    .AddJwtServerAuthentication(builder.Configuration, builder.Environment, "gateway")
    .AddAuthorization();

builder.Services.AddMemoryCache();

builder.Services
    .AddOptions<GatewayEndpointOptions>()
    .Bind(builder.Configuration.GetSection("Gateway"))
    .ValidateOnStart();

builder.Services
    .AddOptions<ServiceEndpointRegistryOptions>()
    .Bind(builder.Configuration.GetSection("ServiceEndpoints"))
    .ValidateOnStart();

builder.Services.AddSingleton<IServiceUrlResolver, ServiceUrlResolver>();

builder.Services
    .AddOptions<SwaggerAggregationOptions>()
    .Bind(builder.Configuration.GetSection("SwaggerAggregation"))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<SwaggerAggregationOptions>, SwaggerAggregationOptionsValidator>();

builder.Services.AddHttpClient(nameof(SwaggerDocumentRewriter));
builder.Services.AddHttpClient(nameof(GatewayDocsProbeService));

builder.Services.AddSingleton<ISwaggerDocumentRewriter, SwaggerDocumentRewriter>();
builder.Services.AddSingleton<GatewaySwaggerUiConfigurator>();
builder.Services.AddSingleton<IGatewayDocsProbeService, GatewayDocsProbeService>();

// IMiddlewares must be register
builder.Services.AddTransient<DocsAccessMiddleware>();
builder.Services.AddTransient<DynamicRequestBodyLimitMiddleware>();

// Yarp Reverse Proxy
builder.Services
    .AddOptions<ReverseProxyRouteRegistryOptions>()
    .Bind(builder.Configuration.GetSection("ReverseProxyRoutes"))
    .ValidateOnStart();
builder.Services.AddSingleton<ReverseProxyConfigBuilder>();
builder.Services.AddSingleton<IProxyConfigProvider>(sp =>
{
    var configBuilder = sp.GetRequiredService<ReverseProxyConfigBuilder>();
    (IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters) = configBuilder.Build();

    return new CustomInMemoryConfigProvider(routes, clusters);
});
builder.Services.AddReverseProxy();

var hcBuilder = builder.Services.AddHealthChecks();
hcBuilder.AddCheck("self-check", () => HealthCheckResult.Healthy(), tags: ["dependencies"]);

// Swagger
if (!builder.Environment.IsHostProduction())
{
    SwaggerConfigurationHelper.Configure(builder.Services, $"{ApplicationIdentifier.AppName} API", "v1", "gateway");
}

// =======================
// Build
// =======================
try
{
    Log.Information("Configuring web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    var app = builder.Build();

    Log.Information("Starting web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    // =======================
    // Configure (Middleware)
    // =======================

    #region ForwardedHeaderWithPolicies

    var gatewayPolicies = app.Services.GetRequiredService<IOptions<GatewayPolicyOptions>>().Value;
    var forwardedHeadersOptions = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
    foreach (string proxyIp in gatewayPolicies.TrustedProxyIps)
    {
        if (System.Net.IPAddress.TryParse(proxyIp, out var ip))
        {
            forwardedHeadersOptions.KnownProxies.Add(ip);
        }
    }

    app.UseForwardedHeaders(forwardedHeadersOptions);

    #endregion

    #region Tracing for Public Network Apps

    app.UseCorrelationId();
    app.UseMiddleware<ApiRequestResponseLoggingMiddleware>();

    #endregion

    app.UseCustomExceptionHandler();

    if (app.Environment.IsHostProduction()) // client must be https
    {
        app.UseHsts();
    }

    app.UseMiddleware<SearchEngineAgentMiddleware>(); // robot.txt

    app.UseStaticFiles(); // direct download files

    // no-localization -> response from backend

    app.UseRouting();

    app.UseDefaultCorsSettings("default");
    //app.UseCors("GatewayCorsPolicy");

    //app.UseRateLimiter(); // anonymous user jwt verify load

    app.UseAuthentication();
    app.UseAuthorization();
    // no-tenant-checker -> response from backend

    app.UseMiddleware<DocsAccessMiddleware>();
    app.UseMiddleware<DynamicRequestBodyLimitMiddleware>();

    if (!app.Environment.IsHostProduction())
    {
        app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/swagger.json"; });
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";

            using var scope = app.Services.CreateScope();
            var uiConfigurator = scope.ServiceProvider.GetRequiredService<GatewaySwaggerUiConfigurator>();
            var endpoints = uiConfigurator.GetEndpoints();

            foreach (var endpoint in endpoints)
            {
                c.SwaggerEndpoint(endpoint.JsonUrl, endpoint.Name);
            }
        });
    }

    #region Endpoints

    app.MapGet("/", async (
        IServiceUrlResolver urlResolver,
        IOptions<SwaggerAggregationOptions> swaggerOptions,
        IGatewayDocsProbeService probeService,
        CancellationToken cancellationToken) =>
    {
        var rootProbeTasks = swaggerOptions.Value.Services.Select(async service => new { service.DisplayName, Probe = await probeService.ProbeEndpointAsync(urlResolver.BuildServiceUrl(service.Key, "/"), cancellationToken) });

        var rootProbes = await Task.WhenAll(rootProbeTasks);

        string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        string appVersion = !string.IsNullOrWhiteSpace(buildNumber) ? $"v1.0.{buildNumber}" : "v1.0.0";

        var lines = new List<string> { $"{NameConsts.SolutionName.ToUpper()} | {ApplicationIdentifier.AppName} | {ApplicationIdentifier.AppId} | {builder.Environment.EnvironmentName} | {appVersion}", "", "Service '/' outputs:" };

        foreach (var item in rootProbes)
        {
            string status = item.Probe?.IsSuccess == true ? "OK" : "FAIL";
            string code = item.Probe?.StatusCode?.ToString() ?? "-";
            string body = item.Probe?.ResponseSnippet ?? item.Probe?.ErrorMessage ?? "No response";

            lines.Add($"- {item.DisplayName} [{status}] ({code}) => {body}");
        }

        return Results.Text(string.Join(Environment.NewLine, lines), "text/plain; charset=utf-8");
    });

    app.MapGet("/gateway-info", () => Results.Ok(new { name = "Hhs.Gateway.BackOffice.Http.Host", status = "running", nowUtc = DateTime.UtcNow }));

    app.MapGet("/docs", async (
        IServiceUrlResolver urlResolver,
        IOptions<SwaggerAggregationOptions> swaggerOptions,
        IGatewayDocsProbeService probeService,
        CancellationToken cancellationToken) =>
    {
        string gatewayBaseUrl = urlResolver.GetGatewayPublicOrigin();

        var liveDefinitions = swaggerOptions.Value.Services.Select(x => new GatewayServiceLiveInfo { Definition = x }).ToList();

        foreach (var item in liveDefinitions)
        {
            item.ServiceHealth = await probeService.ProbeEndpointAsync(
                urlResolver.BuildServiceUrl(item.Definition.Key, item.Definition.ServiceHealthPath),
                cancellationToken);

            item.GatewayHealth = await probeService.ProbeEndpointAsync(
                urlResolver.BuildGatewayUrl(item.Definition.GatewayHealthPath),
                cancellationToken);

            item.ServiceRoot = await probeService.ProbeEndpointAsync(
                urlResolver.BuildServiceUrl(item.Definition.Key, "/"),
                cancellationToken);

            item.GatewayRoot = await probeService.ProbeEndpointAsync(
                urlResolver.BuildGatewayUrl($"/{item.Definition.Key}"),
                cancellationToken);
        }

        string html = GatewayDocsBuilder.BuildDocsHtml(gatewayBaseUrl, liveDefinitions, urlResolver);
        return Results.Content(html, "text/html; charset=utf-8");
    });

    app.MapGet("/openapi-proxy/{serviceKey}", async (
        string serviceKey,
        HttpContext context,
        IOptions<SwaggerAggregationOptions> swaggerOptions,
        ISwaggerDocumentRewriter rewriter,
        CancellationToken cancellationToken) =>
    {
        var service = swaggerOptions.Value.Services
            .FirstOrDefault(x => x.Key.Equals(serviceKey, StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            return Results.NotFound(new { message = $"Swagger service tanımı bulunamadı: {serviceKey}" });
        }

        string gatewayBaseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        string rewrittenJson = await rewriter.GetRewrittenDocumentAsync(service, gatewayBaseUrl, cancellationToken);

        return Results.Text(rewrittenJson, "application/json");
    });

    #endregion

    // HealthChecks
    app.UseHealthChecks("/StartupCheck", new HealthCheckOptions { Predicate = r => r.Tags.Contains("dependencies"), ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
    app.UseHealthChecks("/LivenessCheck", new HealthCheckOptions { Predicate = r => r.Name.Equals("self-check") });
    app.UseHealthChecks("/ReadinessCheck", new HealthCheckOptions { Predicate = r => r.Tags.Contains("dependencies"), ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });

    // Yarp ReverseProxy
    app.MapReverseProxy();

    // Shutdown hook
    var lifetime = app.Lifetime;
    lifetime.ApplicationStopping.Register(() => { Console.WriteLine("Stopping web host ({0})...", ApplicationIdentifier.AppName); });

    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", ApplicationIdentifier.AppName);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// using HsnSoft.Base.AspNetCore.Serilog;
// using HsnSoft.Base.Tracing;
// using Microsoft.AspNetCore;
// using Serilog;
//
// namespace Hhs.Gateway.Commercial;
//
// public static class Program
// {
//     public static async Task<int> Main(string[] args)
//     {
//         string workspace = typeof(Startup).Namespace;
//         ApplicationIdentifier.AppId = Guid.NewGuid().ToString("N");
//         ApplicationIdentifier.AppName = workspace?[(workspace.IndexOf('.') + 1)..];
//
//         Log.Logger = SerilogConfigurationHelper.ConfigureConsoleLogger(GetConfiguration());
//
//         try
//         {
//             Log.Information("Configuring web host ({ApplicationContext})...", ApplicationIdentifier.AppName);
//             var host = CreateHostBuilder(args);
//
//             Log.Information("Starting web host ({ApplicationContext})...", ApplicationIdentifier.AppName);
//             await host.RunAsync();
//
//             return 0;
//         }
//         catch (Exception ex)
//         {
//             Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", ApplicationIdentifier.AppName);
//             return 1;
//         }
//         finally
//         {
//             await Log.CloseAndFlushAsync();
//         }
//     }
//
//     private static IWebHost CreateHostBuilder(string[] args) =>
//         WebHost.CreateDefaultBuilder(args)
//             .CaptureStartupErrors(false)
//             .ConfigureKestrel((context, options) =>
//             {
//                 options.Limits.MaxRequestBufferSize = long.MaxValue;
//                 options.Limits.MaxRequestBodySize = long.MaxValue;
//
//                 var env = context.HostingEnvironment;
//                 if (!env.IsDevelopment()) return;
//
//                 options.ListenAnyIP(7200);
//                 options.ListenAnyIP(7201, listenOptions =>
//                 {
//                     listenOptions.UseHttps("../../etc/dev-cert/localhost.pfx", "e8202f07-66e5-4619-be07-72ba76fde97f");
//                 });
//             })
//             .ConfigureAppConfiguration(x => x.AddConfiguration(GetConfiguration()))
//             .UseStartup<Startup>()
//             .UseContentRoot(Directory.GetCurrentDirectory())
//             .ConfigureLogging(logging =>
//             {
//                 logging.ClearProviders();
//                 logging.AddSerilog(Log.Logger);
//             })
//             .Build();
//
//     private static IConfiguration GetConfiguration() =>
//         new ConfigurationBuilder()
//             .SetBasePath(Directory.GetCurrentDirectory())
//             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//             .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json")
//             .AddJsonFile($"Configurations/ocelot.json", optional: false, reloadOnChange: true)
//             .AddJsonFile($"Configurations/ocelot.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: false, reloadOnChange: true)
//             .AddEnvironmentVariables()
//             .Build();
// }