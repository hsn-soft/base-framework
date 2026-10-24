using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.AspNetCore.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Last line of defence: every unhandled exception becomes a <c>BaseResponse</c> with the status and the error code the mapper decided.
/// An error the application expects (<see cref="KnownExceptions"/>) is a client status (4xx) and no log of its own: the response log says
/// it as a Warning. Any other exception is a failure of the service: 500, an Error log with its stack here, and an Error response log.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IApiExceptionMapper mapper,
    IApiResponseWriter writer,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        (int status, string? errorCode, System.Collections.Generic.List<string> messages) = mapper.Map(exception, environment);

        // the request log says how this request ended and why
        httpContext.Items[RequestLoggingMiddleware.HandledExceptionKey] = exception;

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, httpContext.Request.Path, status);
        }

        if (httpContext.Response.HasStarted)
        {
            // too late to say anything else: the connection is cut by the server
            return false;
        }

        await writer.WriteErrorAsync(httpContext, status, messages, errorCode).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(EventId = 5201, EventName = "GLOBAL_EXCEPTION_HANDLER", Level = LogLevel.Error, Message = "Unhandled exception on {Path}: status {Status}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, PathString path, int status);
}
