using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.Responses;

/// <summary>Writes a <see cref="BaseResponse"/> by hand, for the places that are not an MVC action (middleware, the exception handler).</summary>
public interface IApiResponseWriter
{
    Task WriteErrorAsync(HttpContext context, int statusCode, IEnumerable<string> messages, string? errorCode = null);

    Task WriteSuccessAsync<T>(HttpContext context, int statusCode, T? payload, IEnumerable<string> messages);
}

public sealed class ApiResponseWriter : IApiResponseWriter
{
    /// <summary>How every answer is written: camel case, no nulls, enums as text.</summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task WriteErrorAsync(HttpContext context, int statusCode, IEnumerable<string> messages, string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messages);

        BaseResponse body = new()
        {
            StatusCode = statusCode,
            StatusMessages = [.. messages.Distinct()],
            TraceId = ResponseTrace.Of(context),
            ErrorCode = errorCode,
        };
        await WriteAsync(context, statusCode, JsonSerializer.Serialize(body, s_jsonOptions)).ConfigureAwait(false);
    }

    public async Task WriteSuccessAsync<T>(HttpContext context, int statusCode, T? payload, IEnumerable<string> messages)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messages);

        BaseResponse<T> body = new()
        {
            StatusCode = statusCode,
            StatusMessages = [.. messages.Distinct()],
            TraceId = ResponseTrace.Of(context),
            Payload = payload,
        };
        await WriteAsync(context, statusCode, JsonSerializer.Serialize(body, s_jsonOptions)).ConfigureAwait(false);
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string json)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();

        // Clear took the header the middleware had set: it says in which language the error is told
        if (context.RequestServices?.GetService<ILanguageAccessor>() is { } languages)
        {
            context.Response.Headers.ContentLanguage = languages.Current;
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(json, CancellationToken.None).ConfigureAwait(false);
    }
}
