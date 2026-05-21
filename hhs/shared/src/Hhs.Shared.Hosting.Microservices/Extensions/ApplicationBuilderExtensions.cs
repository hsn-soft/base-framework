using Hhs.Shared.Hosting.Microservices.Middlewares;
using HsnSoft.Base.AspNetCore.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.Shared.Hosting.Microservices.Extensions;

public static class ApplicationBuilderExtensions
{
    extension(WebApplication app)
    {
        public void UseMicroserviceHosting()
        {
            app.UseForwardedHeaders();

            // FILTER 04 : Registered GlobalApiExceptionHandler in service collections
            app.UseExceptionHandler();

            // FILTER 01 : not-found body -> add body ( No method, wrong route )
            app.UseStatusCodePages(async statusCodeContext =>
            {
                var http = statusCodeContext.HttpContext;

                if (http.Response.HasStarted)
                    return;

                if (http.Response.ContentLength > 0)
                    return;

                string contentType = http.Response.ContentType ?? string.Empty;

                if (contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
                    contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var writer = http.RequestServices.GetRequiredService<IApiResponseWriter>();
                var statusProvider = http.RequestServices.GetRequiredService<IStatusMessageProvider>();

                await writer.WriteErrorAsync(
                    http,
                    http.Response.StatusCode,
                    [statusProvider.GetMessage(http.Response.StatusCode)],
                    $"http_{http.Response.StatusCode}");
            });
        }

        public void UseUserTenantChecker() => app.UseMiddleware<UserTenantCheckerMiddleware>();
    }
}