using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.Sample.Api.Hosting.Exceptions;

/// <summary>Maps framework exceptions to the HTTP status code, error code and messages of the unified error response.</summary>
public sealed class ApiExceptionMapper(IStatusMessageProvider statusMessageProvider) : IApiExceptionMapper
{
    public (int StatusCode, string ErrorCode, List<string> Messages) Map(Exception exception, IHostEnvironment environment)
    {
        int code = StatusCodes.Status500InternalServerError;
        string errorCode = null;
        var messages = new List<string>();

        switch (exception)
        {
            case UnauthorizedAccessException:
                code = StatusCodes.Status401Unauthorized;
                break;

            case EntityNotFoundException notFound:
                code = StatusCodes.Status404NotFound;
                messages.Add(notFound.Message);
                break;

            case ArgumentException argumentException:
                code = StatusCodes.Status400BadRequest;
                messages.Add(argumentException.Message);
                break;

            case BusinessException business:
                code = StatusCodes.Status400BadRequest;
                errorCode = business.ErrorCode;
                messages.Add(business.Message);
                messages.AddRange(business.GetDictionaryDataList().Select(x => $"{x.Key}: {x.Value}"));
                break;

            case DomainException domain:
                code = StatusCodes.Status400BadRequest;
                messages.Add(domain.Message);
                messages.AddRange(domain.GetDictionaryDataList().Select(x => $"{x.Key}: {x.Value}"));
                break;

            case BaseHttpException http:
                code = http.HttpStatusCode;
                messages.Add(http.Message);
                break;
        }

        if (messages.Count == 0 || messages.All(string.IsNullOrWhiteSpace))
        {
            messages = [statusMessageProvider.GetMessage(code)];
        }

        if (!environment.IsProduction() && exception.InnerException != null)
        {
            messages.AddRange(exception.InnerException.GetMessages());
        }

        return (code, errorCode, messages);
    }
}
