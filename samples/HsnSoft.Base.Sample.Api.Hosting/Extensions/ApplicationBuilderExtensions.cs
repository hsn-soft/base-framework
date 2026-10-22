using HsnSoft.Base.AspNetCore.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Sample.Api.Hosting.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>Runs <see cref="Exceptions.GlobalApiExceptionHandler"/> and gives bodyless 404/405/... responses the unified error body.</summary>
    public static WebApplication UseCustomExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseStatusCodePages(async context =>
        {
            var http = context.HttpContext;
            if (http.Response.HasStarted || http.Response.ContentLength > 0)
            {
                return;
            }

            var writer = http.RequestServices.GetRequiredService<IApiResponseWriter>();
            var statusMessageProvider = http.RequestServices.GetRequiredService<IStatusMessageProvider>();

            await writer.WriteErrorAsync(http, http.Response.StatusCode, [statusMessageProvider.GetMessage(http.Response.StatusCode)], $"http_{http.Response.StatusCode}");
        });

        return app;
    }
}
