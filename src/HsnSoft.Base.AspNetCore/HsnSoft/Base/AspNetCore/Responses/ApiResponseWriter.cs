using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Responses;

public sealed class ApiResponseWriter : IApiResponseWriter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Converters = { new JsonStringEnumConverter() } };

    public async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        IEnumerable<string> messages,
        string? errorCode = null)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = statusCode;

        var body = new BaseResponse
        {
            StatusCode = statusCode,
            StatusMessages = messages.Distinct().ToList(),
            TraceId = context.TraceIdentifier,
            ErrorCode = errorCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, s_jsonOptions));
    }

    public async Task WriteSuccessAsync<T>(
        HttpContext context,
        int statusCode,
        T? payload,
        IEnumerable<string> messages)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = statusCode;

        var body = new BaseResponse<T> { StatusCode = statusCode, StatusMessages = messages.Distinct().ToList(), TraceId = context.TraceIdentifier, Payload = payload };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, s_jsonOptions));
    }
}