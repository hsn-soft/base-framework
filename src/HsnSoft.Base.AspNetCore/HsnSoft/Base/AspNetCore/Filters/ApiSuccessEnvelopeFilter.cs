using System.Net;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HsnSoft.Base.AspNetCore.Filters;

public sealed class ApiSuccessEnvelopeFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        switch (context.Result)
        {
            case FileResult:
            case ChallengeResult:
            case ForbidResult:
            case RedirectResult:
            case RedirectToActionResult:
            case RedirectToRouteResult:
            case RedirectToPageResult:
                await next();
                return;
        }

        if (context.Result is ObjectResult objectResult)
        {
            if (objectResult.Value is BaseResponse)
            {
                await next();
                return;
            }

            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;

            objectResult.Value = new BaseResponse<object>
            {
                StatusCode = statusCode,
                StatusMessages = [GetDefaultMessage(statusCode)],
                TraceId = context.HttpContext.TraceIdentifier,
                Payload = objectResult.Value
            };

            await next();
            return;
        }

        if (context.Result is EmptyResult)
        {
            context.Result = new ObjectResult(new BaseResponse
            {
                StatusCode = StatusCodes.Status200OK,
                StatusMessages = ["Success"],
                TraceId = context.HttpContext.TraceIdentifier
            })
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        await next();
    }

    private static string GetDefaultMessage(int statusCode) => statusCode switch
    {
        (int)HttpStatusCode.OK => "Success",
        (int)HttpStatusCode.Created => "Created",
        (int)HttpStatusCode.Accepted => "Accepted",
        (int)HttpStatusCode.NoContent => "No content",
        _ => "Success"
    };
}