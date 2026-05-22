using Hhs.AuthServer;
using Hhs.AuthServer.Application;
using Hhs.AuthServer.Application.Contracts.AuthDomain.Interfaces;
using Hhs.AuthServer.Application.Localization;
using Hhs.AuthServer.Options;
using Hhs.IdentityService.EntityFrameworkCore;
using Hhs.Shared.Contracts.Cache.ServicePermissions;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using Hhs.Shared.Hosting.Microservices.Extensions;
using Hhs.Shared.Hosting.Middlewares;
using HsnSoft.Base.AspNetCore.Localization;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Swashbuckle;
using HsnSoft.Base.Tracing;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Serilog;

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
builder.Services
    .AddOptions<AuthServerPolicyOptions>()
    .Bind(builder.Configuration.GetSection("AuthServerPolicies"))
    .ValidateOnStart();

builder.WebHost.ConfigureKestrel((_, options) =>
{
    var policyOptions = builder.Configuration.GetSection("AuthServerPolicies").Get<AuthServerPolicyOptions>() ?? new AuthServerPolicyOptions();

    options.Limits.MaxRequestBodySize = policyOptions.Kestrel.MaxRequestBodySizeBytes;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(policyOptions.Kestrel.RequestHeadersTimeoutSeconds);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(policyOptions.Kestrel.KeepAliveTimeoutSeconds);
    options.Limits.MaxRequestHeaderCount = policyOptions.Kestrel.MaxRequestHeaderCount;
    options.Limits.MaxRequestHeadersTotalSize = policyOptions.Kestrel.MaxRequestHeadersTotalSizeBytes;

    if (!builder.Environment.IsDevelopment()) return;

    options.ListenAnyIP(7100);
    options.ListenAnyIP(7101, listenOptions =>
    {
        listenOptions.UseHttps("../../etc/dev-cert/localhost.pfx", "e8202f07-66e5-4619-be07-72ba76fde97f");
    });
});
builder.Services.AddCors(options =>
{
    var policyOptions = builder.Configuration.GetSection("AuthServerPolicies").Get<AuthServerPolicyOptions>() ?? new AuthServerPolicyOptions();

    options.AddPolicy("AuthServerCorsPolicy", cors =>
    {
        if (policyOptions.AllowedCorsOrigins.Length > 0)
        {
            cors.WithOrigins(policyOptions.AllowedCorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            cors.DisallowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
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
//         var policy = httpContext.RequestServices.GetRequiredService<IOptions<AuthServerPolicyOptions>>().Value;
//         var category = ServerRouteClassifier.GetRateCategory(httpContext, policy);
//         string requesterKey = ServerRouteClassifier.GetPartitionKey(httpContext);
//
//         return category switch
//         {
//             ServerRateCategory.IdentityLogin =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"identity-login:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.IdentityLoginPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }),
//
//             ServerRateCategory.IdentityAuth =>
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
//             _ =>
//                 RateLimitPartition.GetFixedWindowLimiter(
//                     partitionKey: $"global:{requesterKey}",
//                     factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = policy.RateLimits.GlobalPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = policy.RateLimits.QueueLimit, AutoReplenishment = true })
//         };
//     });
// });

// =======================
// ConfigureServices
// =======================
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

builder.Services.AddMicroserviceHosting(builder.Configuration, typeof(Program))
    .AddRequestResponseLogger()
    .AddJwtServerAuthentication(builder.Configuration, builder.Environment, "audience-service-identity")
    .AddCustomAuthorization(IdentityServicePermissions.GetAll())
    .AddMicroserviceUserTenantChecker()
    .AddHostingHealthChecks(builder.Configuration, "auth-server", checkRedis: true,
        checkPostgresql: true, postgresqlConnectionName: EfCoreDbProperties.ConnectionStringName);

builder.Services.AddServiceApplicationConfiguration(builder.Configuration);
builder.Services .AddServiceEfCoreDatabaseConfiguration(builder.Configuration, !builder.Environment.IsHostProduction());


// old services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RedisService>();

// new services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// auth services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();


// Swagger
if (!builder.Environment.IsHostProduction())
{
    SwaggerConfigurationHelper.ConfigureWithBearer(builder.Services,
        "Please enter a valid token. Token audiences contains audience-service-identity",
        $"{ApplicationIdentifier.AppName} API");
}

// =======================
// Build
// =======================
try
{
    Log.Information("Configuring web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    var app = builder.Build();

    Log.Information("Starting web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    using (var scope = app.Services.CreateScope())
    {
        // optional init - seed
    }

    // =======================
    // Configure (Middleware)
    // =======================

    if (!app.Environment.IsHostProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApplicationIdentifier.AppName} API");
        });
    }

    #region ForwardedHeaderWithPolicies

    var gatewayPolicies = app.Services.GetRequiredService<IOptions<AuthServerPolicyOptions>>().Value;
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

    app.UseExceptionHandler();

    app.UseStatusCodePages(async statusCodeContext =>
    {
        var http = statusCodeContext.HttpContext;

        if (http.Response.ContentLength > 0)
            return;

        var writer = http.RequestServices.GetRequiredService<IApiResponseWriter>();
        var statusProvider = http.RequestServices.GetRequiredService<IStatusMessageProvider>();

        await writer.WriteErrorAsync(
            http,
            http.Response.StatusCode,
            [statusProvider.GetMessage(http.Response.StatusCode)]);
    });

    app.UseCors("AuthServerCorsPolicy");
    app.UseSearchEngineAgent();

    app.ConfigureLocalizedModelValidator(typeof(AuthServerResource));
    app.UseMiddleware<BaseLocalizationMiddleware>();

    app.UseRouting();

    app.UseCorrelationId();
    app.UseMiddleware<ApiRequestResponseLoggingMiddleware>();

    // app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseUserTenantChecker();

    #region Endpoints

    string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
    string appVersion = !string.IsNullOrWhiteSpace(buildNumber) ? $"v1.0.{buildNumber}" : "v1.0.0";
    app.MapGet("/", () => Results.Text($"{NameConsts.SolutionName.ToUpper()} | {ApplicationIdentifier.AppName} | {ApplicationIdentifier.AppId} | {builder.Environment.EnvironmentName} | {appVersion}"))
        .AllowAnonymous();

    app.MapControllers();

    #endregion

    // HealthChecks
    app.UseHostingHealthChecks();

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