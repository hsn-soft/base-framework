using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Decides what an exception means for the client: the status, the error code and the messages. Register your own to add the exceptions of a product.</summary>
public interface IApiExceptionMapper
{
    ApiError Map(Exception exception, IHostEnvironment environment);
}

/// <summary>What the client is told of an exception: the status, the error code, the messages and the messages (for a request with fields that were wrong: one for each field).</summary>
public sealed record ApiError(int StatusCode, string? ErrorCode, List<string> Messages);

public sealed class ApiExceptionMapper(IStatusMessageProvider statusMessages, IOptions<ApiErrorOptions> errorOptions, IMessageLocalizer? localizer = null) : IApiExceptionMapper
{
    public ApiError Map(Exception exception, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(environment);

        // every field that was wrong, each told in the language of the request
        if (exception is ValidationFailedException failed)
        {
            return new ApiError(StatusCodes.Status400BadRequest, "validation_error", [localizer?.Find("Validation:Failed") ?? "Validation failed.", .. failed.Errors.Select(Say)]);
        }

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
        // what the client reads is in its language when the error names a text (the log keeps the message as it was written)
        string? message = showsMessage ? Localized(exception) ?? (string.IsNullOrWhiteSpace(exception.Message) ? null : exception.Message) : null;
        List<string> messages = [message ?? statusMessages.GetMessage(status)];
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            messages.Add(localizer?.Find("Base:ErrorCode", errorCode) ?? $"Error code: {errorCode}");
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

        return new ApiError(status, errorCode, messages);
    }

    private string Say(ValidationError error)
        => error.Key is { Length: > 0 } key && localizer?.Find(key, [.. error.Args ?? []]) is { } text ? text : error.Message;

    private string? Localized(Exception exception)
        => localizer is not null && exception is ILocalizableError { MessageKey: { Length: > 0 } key } error ? localizer.Find(key, [.. error.MessageArgs]) : null;

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
