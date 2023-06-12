using HsnSoft.Base.AspNetCore.Localization;
using HsnSoft.Base.AspNetCore.Security;
using HsnSoft.Base.AspNetCore.Security.Claims;
using HsnSoft.Base.AspNetCore.Tracing;

namespace Microsoft.AspNetCore.Builder;

public static class BaseApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLocalizationMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BaseLocalizationMiddleware>();
    }

    public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BaseCorrelationIdMiddleware>();
    }

    public static IApplicationBuilder UseBaseClaimsMapMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BaseClaimsMapMiddleware>();
    }

    public static IApplicationBuilder UseBaseSecurityHeadersMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BaseSecurityHeadersMiddleware>();
    }
}