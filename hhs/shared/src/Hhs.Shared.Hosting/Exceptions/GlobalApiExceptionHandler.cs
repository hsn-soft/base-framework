using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Logging.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Hhs.Shared.Hosting.Exceptions;

public sealed class GlobalApiExceptionHandler : IExceptionHandler
{
    private readonly IApiExceptionMapper _mapper;
    private readonly IApiResponseWriter _writer;
    private readonly IFrameworkLogger _frameworkLogger;
    private readonly IHostEnvironment _env;

    public GlobalApiExceptionHandler(
        IApiExceptionMapper mapper,
        IApiResponseWriter writer,
        IFrameworkLogger frameworkLogger,
        IHostEnvironment env)
    {
        _mapper = mapper;
        _writer = writer;
        _frameworkLogger = frameworkLogger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int statusCode, string errorCode, List<string> messages) = _mapper.Map(exception, _env);

        // extra framework error trace log
        // _frameworkLogger.FrameworkErrorLog(new FrameworkLogModel
        // {
        //     Message = exception.Message,
        //     CorrelationId = httpContext.GetCorrelationId(),
        //     Facility = "GlobalExceptionHandler",
        //     Reference = new { Exception = exception.ToString(), StatusCode = statusCode, ErrorCode = errorCode, Path = httpContext.Request.Path.ToString() },
        //     StackTrace = null
        // });

        if (httpContext.Response.HasStarted)
            return false;

        httpContext.Response.Clear();

        await _writer.WriteErrorAsync(httpContext, statusCode, messages, errorCode);
        return true;
    }
}