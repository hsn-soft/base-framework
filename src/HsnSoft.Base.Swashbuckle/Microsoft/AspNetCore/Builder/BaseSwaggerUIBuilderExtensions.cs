using System;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Microsoft.AspNetCore.Builder;

public static class BaseSwaggerUIBuilderExtensions
{
    public static IApplicationBuilder UseBaseSwaggerUI(
        this IApplicationBuilder app,
        Action<SwaggerUIOptions> setupAction = null)
    {
        // var resolver = app.ApplicationServices.GetService<ISwaggerHtmlResolver>();

        return app.UseSwaggerUI(options =>
        {
            // options.InjectJavascript("ui/base.js");
            // options.InjectJavascript("ui/base.swagger.js");
            // options.IndexStream = () => resolver.Resolver();

            setupAction?.Invoke(options);
        });
    }
}