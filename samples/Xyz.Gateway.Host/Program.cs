using Xyz.Gateway.Host;
using Xyz.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSharedWebApi("gateway")
    .AddSharedJwtAuthentication(builder.Configuration)
    .AddSharedRedis(builder.Configuration)
    .Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimit"));

// The routes and the downstream addresses are configuration (ReverseProxy section), not code.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// the rate limit needs the caller (authentication) but must also count requests that authorization will reject
app.UseSharedPipeline(pipeline => pipeline.UseMiddleware<RedisRateLimitMiddleware>());
app.MapSharedHealth();
app.MapReverseProxy();

await app.RunAsync();
