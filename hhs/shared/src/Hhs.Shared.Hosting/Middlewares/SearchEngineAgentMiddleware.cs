using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Hhs.Shared.Hosting.Middlewares;

public sealed class SearchEngineAgentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SearchEngineAgentMiddleware(
        RequestDelegate next,
        IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/robots.txt"))
        {
            await _next(context);
            return;
        }

        string robotsTxtPath = Path.Combine(_env.ContentRootPath, "robots.txt");
        string output = "User-agent: *  \nDisallow: /";
        if (File.Exists(robotsTxtPath))
        {
            output = await File.ReadAllTextAsync(robotsTxtPath);
        }

        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(output);
    }
}