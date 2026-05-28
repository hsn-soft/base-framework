using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HsnSoft.Base.AspNetCore.Responses;

public sealed class UnifiedApiResponseFilter : IAsyncResultFilter
{
    private readonly IStatusMessageProvider _statusMessageProvider;

    public UnifiedApiResponseFilter(IStatusMessageProvider statusMessageProvider)
    {
        _statusMessageProvider = statusMessageProvider;
    }

    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        if (context.HttpContext.Response.HasStarted)
        {
            await next();
            return;
        }

        switch (context.Result)
        {
            case ObjectResult objectResult:
                {
                    if (objectResult.Value is BaseResponse)
                    {
                        await next();
                        return;
                    }

                    if (objectResult.Value is ProblemDetails or ValidationProblemDetails)
                    {
                        await next();
                        return;
                    }

                    var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;

                    context.Result = new ObjectResult(new BaseResponse<object> { StatusCode = statusCode, StatusMessages = [_statusMessageProvider.GetMessage(statusCode)], TraceId = context.HttpContext.TraceIdentifier, Payload = objectResult.Value }) { StatusCode = statusCode };

                    break;
                }

            case JsonResult jsonResult:
                {
                    if (jsonResult.Value is BaseResponse)
                    {
                        await next();
                        return;
                    }

                    var statusCode = jsonResult.StatusCode ?? StatusCodes.Status200OK;

                    context.Result = new ObjectResult(new BaseResponse<object> { StatusCode = statusCode, StatusMessages = [_statusMessageProvider.GetMessage(statusCode)], TraceId = context.HttpContext.TraceIdentifier, Payload = jsonResult.Value }) { StatusCode = statusCode };

                    break;
                }

            case StatusCodeResult statusCodeResult:
                {
                    var statusCode = statusCodeResult.StatusCode;

                    context.Result = new ObjectResult(new BaseResponse { StatusCode = statusCode, StatusMessages = [_statusMessageProvider.GetMessage(statusCode)], TraceId = context.HttpContext.TraceIdentifier }) { StatusCode = statusCode };

                    break;
                }

            case EmptyResult:
                {
                    context.Result = new ObjectResult(new BaseResponse
                    {
                        StatusCode = StatusCodes.Status204NoContent, StatusMessages = [_statusMessageProvider.GetMessage(StatusCodes.Status204NoContent)], TraceId = context.HttpContext.TraceIdentifier
                    }) { StatusCode = StatusCodes.Status204NoContent };

                    break;
                }

            case FileResult:
            case RedirectResult:
            case RedirectToActionResult:
            case RedirectToRouteResult:
                break;
        }

        await next();
    }
}