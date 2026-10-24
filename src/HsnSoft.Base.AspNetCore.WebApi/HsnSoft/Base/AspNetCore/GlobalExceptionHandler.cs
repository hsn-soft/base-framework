using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.AspNetCore.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Last line of defence: every unhandled exception becomes a <c>BaseResponse</c> with the status and the error code the mapper decided.
/// An error the application expects (<see cref="KnownExceptions"/>) is a client status (4xx) and no log of its own: the response log says
/// it as a Warning. Any other exception is a failure of the service: 500, and the Error response log (the only log of the failure) carries its stack.
/// </summary>
public sealed class GlobalExceptionHandler(
    IApiExceptionMapper mapper,
    IApiResponseWriter writer,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        (int status, string? errorCode, System.Collections.Generic.List<string> messages) = mapper.Map(exception, environment);

        // the request log says how this request ended and why
        httpContext.Items[RequestLoggingMiddleware.HandledExceptionKey] = exception;

        if (httpContext.Response.HasStarted)
        {
            // too late to say anything else: the connection is cut by the server
            return false;
        }

        await writer.WriteErrorAsync(httpContext, status, messages, errorCode).ConfigureAwait(false);
        return true;
    }
}
