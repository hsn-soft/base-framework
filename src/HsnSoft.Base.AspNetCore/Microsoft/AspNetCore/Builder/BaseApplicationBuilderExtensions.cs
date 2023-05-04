using HsnSoft.Base.AspNetCore.Security;
using HsnSoft.Base.AspNetCore.Security.Claims;
using HsnSoft.Base.AspNetCore.Tracing;

namespace Microsoft.AspNetCore.Builder;

public static class BaseApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app
            .UseMiddleware<BaseCorrelationIdMiddleware>();
    }

    public static IApplicationBuilder UseBaseClaimsMap(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BaseClaimsMapMiddleware>();
    }

    public static IApplicationBuilder UseBaseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BaseSecurityHeadersMiddleware>();
    }
}