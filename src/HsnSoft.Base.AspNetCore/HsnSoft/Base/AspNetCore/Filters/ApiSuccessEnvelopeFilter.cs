using System;
using System.Collections.Generic;
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

        object? value;
        int statusCode = StatusCodes.Status200OK;

        switch (context.Result)
        {
            case ObjectResult obj:
                value = obj.Value;
                statusCode = obj.StatusCode ?? StatusCodes.Status200OK;
                break;

            case JsonResult json:
                value = json.Value;
                break;

            case EmptyResult:
                value = null;
                break;

            default:
                value = context.Result;
                break;
        }

        if (value != null && IsBaseResponse(value.GetType()))
        {
            await next();
            return;
        }

        string traceId = context.HttpContext.TraceIdentifier;

        object response;

        if (value == null)
        {
            response = new BaseResponse { StatusCode = statusCode, StatusMessages = [GetDefaultMessage(statusCode)], TraceId = traceId };
        }
        else
        {
            var responseType = typeof(BaseResponse<>).MakeGenericType(value.GetType());
            object instance = Activator.CreateInstance(responseType)!;

            responseType.GetProperty(nameof(BaseResponse.StatusCode))!
                .SetValue(instance, statusCode);

            responseType.GetProperty(nameof(BaseResponse.StatusMessages))!
                .SetValue(instance, new List<string> { GetDefaultMessage(statusCode) });

            responseType.GetProperty(nameof(BaseResponse.TraceId))!
                .SetValue(instance, traceId);

            responseType.GetProperty("Payload")!
                .SetValue(instance, value);

            response = instance;
        }

        context.Result = new ObjectResult(response) { StatusCode = statusCode };

        await next();
    }

    private static bool IsBaseResponse(Type type)
    {
        if (type == typeof(BaseResponse))
            return true;

        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BaseResponse<>);
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