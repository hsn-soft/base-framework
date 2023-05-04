using HsnSoft.Base.AspNetCore.Serilog;

namespace Microsoft.AspNetCore.Builder;

public static class BaseAspNetCoreSerilogApplicationBuilderExtensions
{
    public static IApplicationBuilder UseBaseSerilogEnrichers(this IApplicationBuilder app)
    {
        return app
            .UseMiddleware<BaseSerilogMiddleware>();
    }
}
