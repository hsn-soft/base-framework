using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HsnSoft.Base.AspNetCore.Responses;

/// <summary>
/// Puts every result of a controller action into the one response type: an object becomes the payload of a
/// <see cref="BaseResponse{TPayload}"/>, an empty result or a bare status becomes a <see cref="BaseResponse"/>. What already is a
/// <see cref="BaseResponse"/> or a problem, files and redirects are left as they are. A gateway that only forwards what the services
/// answer does not need it.
/// </summary>
public sealed class UnifiedApiResponseFilter(IStatusMessageProvider statusMessages) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (!context.HttpContext.Response.HasStarted)
        {
            string traceId = context.HttpContext.TraceIdentifier;

            switch (context.Result)
            {
                case ObjectResult { Value: not (BaseResponse or ProblemDetails) } objectResult:
                    {
                        int statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
                        context.Result = Wrap(statusCode, traceId, objectResult.Value);
                        break;
                    }

                case JsonResult { Value: not BaseResponse } jsonResult:
                    {
                        int statusCode = jsonResult.StatusCode ?? StatusCodes.Status200OK;
                        context.Result = Wrap(statusCode, traceId, jsonResult.Value);
                        break;
                    }

                case StatusCodeResult statusCodeResult:
                    context.Result = Envelope(statusCodeResult.StatusCode, traceId);
                    break;

                case EmptyResult:
                    context.Result = Envelope(StatusCodes.Status204NoContent, traceId);
                    break;
            }
        }

        await next().ConfigureAwait(false);
    }

    private ObjectResult Wrap(int statusCode, string traceId, object? payload)
        => new(new BaseResponse<object> { StatusCode = statusCode, StatusMessages = [statusMessages.GetMessage(statusCode)], TraceId = traceId, Payload = payload }) { StatusCode = statusCode };

    private ObjectResult Envelope(int statusCode, string traceId)
        => new(new BaseResponse { StatusCode = statusCode, StatusMessages = [statusMessages.GetMessage(statusCode)], TraceId = traceId }) { StatusCode = statusCode };
}
