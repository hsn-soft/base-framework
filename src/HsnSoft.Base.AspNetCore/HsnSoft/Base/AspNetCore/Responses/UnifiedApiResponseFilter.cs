#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HsnSoft.Base.AspNetCore.Responses;

public sealed class UnifiedApiResponseFilter(IApiResponseWriter writer, IStatusMessageProvider statusMessages) : IAsyncResultFilter
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

        if (statusCode is >= 200 and < 300)
        {
            await writer.WriteSuccessAsync(
                context.HttpContext,
                statusCode,
                value,
                new List<string> { statusMessages.GetMessage(statusCode) }
            );
        }
        else
        {
            await writer.WriteErrorAsync(
                context.HttpContext,
                statusCode,
                new List<string> { statusMessages.GetMessage(statusCode) }
            );
        }
    }

    private static bool IsBaseResponse(Type type)
        => type == typeof(BaseResponse) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BaseResponse<>));
}