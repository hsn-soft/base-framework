using HsnSoft.Base.Logging.Serilog;
using Xyz.Shared.Hosting;
using Xyz.Shared.Hosting.Gateways;
using Xyz.Shared.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Xyz.Gateway.Commercial");
builder.WebHost.ConfigureXyzGatewayKestrel();

builder.Services
    .AddXyzGateway(builder.Configuration, "Xyz.Gateway.Commercial API", authenticateUsers: true)
    .AddXyzRedis(builder.Configuration)
    .AddXyzRateLimiting(builder.Configuration);

WebApplication app = builder.Build();

// the rate limit needs the caller (authentication) but must also count requests that authorization will reject
app.UseXyzGateway(authenticateUsers: true, pipeline => pipeline.UseMiddleware<RedisRateLimitMiddleware>());

await app.RunAsync();
