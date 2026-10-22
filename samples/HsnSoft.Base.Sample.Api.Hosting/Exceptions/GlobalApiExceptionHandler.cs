using HsnSoft.Base.AspNetCore.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Sample.Api.Hosting.Exceptions;

/// <summary>Last line of defence: every unhandled exception becomes a unified error response.</summary>
public sealed class GlobalApiExceptionHandler(
    IApiExceptionMapper mapper,
    IApiResponseWriter writer,
    IHostEnvironment environment,
    ILogger<GlobalApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, errorCode, messages) = mapper.Map(exception, environment);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        }

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        await writer.WriteErrorAsync(httpContext, statusCode, messages, errorCode);
        return true;
    }
}
