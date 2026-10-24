using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Decides what an exception means for the client: the status, the error code and the messages. Register your own to add the exceptions of a product.</summary>
public interface IApiExceptionMapper
{
    (int StatusCode, string? ErrorCode, List<string> Messages) Map(Exception exception, IHostEnvironment environment);
}

public sealed class ApiExceptionMapper(IStatusMessageProvider statusMessages, IOptions<ApiErrorOptions> errorOptions) : IApiExceptionMapper
{
    public (int StatusCode, string? ErrorCode, List<string> Messages) Map(Exception exception, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(environment);

        (int status, string? errorCode, bool showsMessage) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, null, false),
            NotFoundException => (StatusCodes.Status404NotFound, null, true),
            ArgumentException or BusinessRuleException or DomainException or BadHttpRequestException => (StatusCodes.Status400BadRequest, null, true),
            BusinessException business => (StatusCodes.Status400BadRequest, business.ErrorCode, true),
            BaseHttpException http => (http.HttpStatusCode, null, true),
            _ => (StatusCodes.Status500InternalServerError, null, false),
        };

        // the text of an unexpected error may leak internals; the others are written for the client
        List<string> messages = [showsMessage && !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : statusMessages.GetMessage(status)];
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            messages.Add($"Error code: {errorCode}");
        }

        messages.AddRange(exception.Data.Cast<DictionaryEntry>().Select(entry => $"{entry.Key}: {entry.Value}"));

        // why and where is for the developer: where the service runs for real (production, uat, stage) the client gets the key and the trace id only
        if (!errorOptions.Value.HidesDetails(environment))
        {
            if (status >= StatusCodes.Status500InternalServerError)
            {
                messages.Add(ExceptionLogFields.ReasonOf(exception));
                messages.Add(ExceptionLogFields.Identifier(exception));
            }
            else
            {
                messages.AddRange(InnerMessages(exception.InnerException));
            }
        }

        return (status, errorCode, messages);
    }

    private static IEnumerable<string> InnerMessages(Exception? exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                yield return current.Message;
            }
        }
    }
}
