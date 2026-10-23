using Xyz.Shared;
using Xyz.Shared.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddXyzApi(builder.Configuration)
    .AddXyzRateLimiting(builder.Configuration);

// The routes and the downstream addresses are configuration (ReverseProxy section), not code.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

WebApplication app = builder.Build();

// the rate limit needs the caller (authentication) but must also count requests that authorization will reject
app.UseXyzApi(pipeline => pipeline.UseMiddleware<RedisRateLimitMiddleware>());
app.MapReverseProxy();

await app.RunAsync();
