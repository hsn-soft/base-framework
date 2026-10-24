using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HsnSoft.Base.AspNetCore.Responses;

/// <summary>
/// Puts every result of a controller action into the one response type: an object becomes the payload of a
/// <see cref="BaseResponse{TPayload}"/>, a bare status becomes a <see cref="BaseResponse"/> (except 204, 205 and 304, which have no body, and an empty result is a 204). What already is a
/// <see cref="BaseResponse"/> or a problem, files and redirects are left as they are. A gateway that only forwards what the services
/// answer does not need it.
/// </summary>
public sealed class UnifiedApiResponseFilter(IStatusMessageProvider statusMessages) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (!context.HttpContext.Response.HasStarted)
        {
            string traceId = ResponseTrace.Of(context.HttpContext);

            switch (context.Result)
            {
                case ObjectResult { Value: not (BaseResponse or ProblemDetails) } objectResult:
                    {
                        int statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
                        RememberShape(context, objectResult.Value);
                        context.Result = Wrap(statusCode, traceId, objectResult.Value);
                        break;
                    }

                case JsonResult { Value: not BaseResponse } jsonResult:
                    {
                        int statusCode = jsonResult.StatusCode ?? StatusCodes.Status200OK;
                        RememberShape(context, jsonResult.Value);
                        context.Result = Wrap(statusCode, traceId, jsonResult.Value);
                        break;
                    }

                // a status that has no body by the rules of HTTP (204, 205, 304) stays as it is: the server refuses to write one
                case StatusCodeResult statusCodeResult when !HasNoBody(statusCodeResult.StatusCode):
                    context.Result = Envelope(statusCodeResult.StatusCode, traceId);
                    break;

                case EmptyResult:
                    context.Result = new StatusCodeResult(StatusCodes.Status204NoContent);
                    break;
            }
        }

        await next().ConfigureAwait(false);
    }

    private static bool HasNoBody(int statusCode) => statusCode is StatusCodes.Status204NoContent or StatusCodes.Status205ResetContent or StatusCodes.Status304NotModified;

    // the request log hides what the class of the answer marks as sensitive, so it needs to know the class
    private static void RememberShape(ResultExecutingContext context, object? payload)
    {
        if (payload is not null)
        {
            context.HttpContext.Items[RequestLoggingMiddleware.ResponseShapeKey] = payload.GetType();
        }
    }

    private ObjectResult Wrap(int statusCode, string traceId, object? payload)
        => new(new BaseResponse<object> { StatusCode = statusCode, StatusMessages = [statusMessages.GetMessage(statusCode)], TraceId = traceId, Payload = payload }) { StatusCode = statusCode };

    private ObjectResult Envelope(int statusCode, string traceId)
        => new(new BaseResponse { StatusCode = statusCode, StatusMessages = [statusMessages.GetMessage(statusCode)], TraceId = traceId }) { StatusCode = statusCode };
}
