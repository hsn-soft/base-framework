using System.Net;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace Hhs.Shared.Hosting.Exceptions;

public sealed class ApiExceptionMapper(IStringLocalizerFactory factory) : IApiExceptionMapper
{
    private readonly IStringLocalizer _localizer = factory.CreateMultiple([typeof(ValidationResource), typeof(SharedResource)]);

    public (int StatusCode, string? ErrorCode, List<string> Messages) Map(Exception ex, IHostEnvironment env)
    {
        int code = StatusCodes.Status500InternalServerError;
        string? errorCode = null;
        var messages = new List<string>();

        if (ex is null) return (code, null, messages);

        switch (ex)
        {
            case UnauthorizedAccessException:
                code = StatusCodes.Status401Unauthorized;
                messages.Add(GetStatusCodeDescription(code));
                break;

            case ArgumentException argEx:
                code = StatusCodes.Status400BadRequest;
                messages.Add(!string.IsNullOrWhiteSpace(argEx.Message) ? argEx.Message : GetStatusCodeDescription(code));
                break;

            // Some logic to handle specific exceptions
            case BusinessException be:
                {
                    code = StatusCodes.Status400BadRequest;
                    errorCode = be.ErrorCode;
                    messages.Add(!string.IsNullOrWhiteSpace(be.Message) ? be.Message : GetStatusCodeDescription(code));

                    if (!string.IsNullOrWhiteSpace(be.ErrorCode)) messages.Add(_localizer[ValidationResourceKeys.ErrorCode, be.ErrorCode]);
                    if (be.Data is { Count: > 0 })
                    {
                        messages.AddRange(be.GetDictionaryDataList()
                            .Select(data => $"{data.Key}: {data.Value}"));
                    }

                    if (!env.IsHostProduction() && be.InnerException != null)
                    {
                        messages.AddRange(be.InnerException.GetMessages());
                    }

                    break;
                }
            case DomainException de:
                {
                    code = StatusCodes.Status400BadRequest;
                    messages.Add(!string.IsNullOrWhiteSpace(de.Message) ? de.Message : GetStatusCodeDescription(code));

                    if (de.Data is { Count: > 0 })
                    {
                        messages.AddRange(de.GetDictionaryDataList()
                            .Select(data => $"{data.Key}: {data.Value}"));
                    }

                    if (!env.IsHostProduction() && de.InnerException != null)
                    {
                        messages.AddRange(de.InnerException.GetMessages());
                    }

                    break;
                }
            case BaseHttpException he:
                {
                    code = he.HttpStatusCode;
                    messages.Add(!string.IsNullOrWhiteSpace(he.Message) ? he.Message : GetStatusCodeDescription(code));

                    if (he.Data is { Count: > 0 })
                    {
                        messages.AddRange(he.GetDictionaryDataList()
                            .Select(data => $"{data.Key}: {data.Value}"));
                    }

                    if (!env.IsHostProduction() && he.InnerException != null)
                    {
                        messages.AddRange(he.InnerException.GetMessages());
                    }

                    break;
                }

            default:
                code = StatusCodes.Status500InternalServerError;
                messages.Add(GetStatusCodeDescription(code));

                if (!env.IsHostProduction() && ex.InnerException != null)
                {
                    messages.AddRange(ex.InnerException.GetMessages());
                }

                break;
        }

        return (code, errorCode, messages);
    }

    private string GetStatusCodeDescription(int statusCode)
    {
        if (statusCode is < 200 or > 520) return string.Empty;

        return (HttpStatusCode)statusCode switch
        {
            HttpStatusCode.BadRequest => _localizer["InvalidModelStateErrorMessage"],
            HttpStatusCode.Unauthorized => _localizer["UnauthorizedRequest"],
            HttpStatusCode.Forbidden => _localizer["ForbiddenRequest"],
            HttpStatusCode.NotFound => _localizer["NotFoundRequest"],
            HttpStatusCode.MethodNotAllowed => _localizer["MethodNotAllowed"],
            HttpStatusCode.RequestTimeout => _localizer["RequestTimeout"],
            HttpStatusCode.UnsupportedMediaType => _localizer["UnsupportedRequestContentType"],
            HttpStatusCode.OK => _localizer["SuccessRequest"],
            _ => _localizer[((HttpStatusCode)statusCode).ToString()]
        };
    }
}