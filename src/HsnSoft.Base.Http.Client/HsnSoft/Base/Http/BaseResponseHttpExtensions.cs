using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;

namespace HsnSoft.Base.Http;

/// <summary>
/// Calls a service that answers with a <see cref="BaseResponse"/> and reads the answer: the payload of a success, or a <see cref="BaseHttpException"/> with the status of the failure and the messages the service gave (the error code is in
/// <c>Exception.Data["errorCode"]</c>). A caller that must tell "the token was refused" from "the call failed" reads <see cref="BaseHttpException.HttpStatusCode"/> (401). The JSON is camelCase, names are not case sensitive and enums are texts.
/// </summary>
public static class BaseResponseHttpExtensions
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    /// <summary>GET; the payload of the answer.</summary>
    public static Task<TPayload?> GetPayloadAsync<TPayload>(this HttpClient client, string path, CancellationToken cancellationToken = default)
        => SendPayloadAsync<TPayload>(client, HttpMethod.Get, path, null, cancellationToken);

    /// <summary>POST with a JSON body; the payload of the answer.</summary>
    public static Task<TPayload?> PostPayloadAsync<TPayload>(this HttpClient client, string path, object? body, CancellationToken cancellationToken = default)
        => SendPayloadAsync<TPayload>(client, HttpMethod.Post, path, body, cancellationToken);

    /// <summary>PUT with a JSON body; the payload of the answer.</summary>
    public static Task<TPayload?> PutPayloadAsync<TPayload>(this HttpClient client, string path, object? body, CancellationToken cancellationToken = default)
        => SendPayloadAsync<TPayload>(client, HttpMethod.Put, path, body, cancellationToken);

    /// <summary>DELETE; nothing is read of the answer of a success.</summary>
    public static async Task DeleteResourceAsync(this HttpClient client, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using HttpResponseMessage response = await client.DeleteAsync(path, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a call and reads the payload of its answer: <see langword="default"/> when the answer has no body (204) or no payload.</summary>
    public static async Task<TPayload?> SendPayloadAsync<TPayload>(this HttpClient client, HttpMethod method, string path, object? body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using HttpRequestMessage request = new(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: s_json);
        }

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        BaseResponse<TPayload>? answer = await ReadAsync<BaseResponse<TPayload>>(response, cancellationToken).ConfigureAwait(false);
        return answer is null ? default : answer.Payload;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        BaseResponse? failure = await ReadAsync<BaseResponse>(response, cancellationToken).ConfigureAwait(false);
        string message = failure is { StatusMessages.Count: > 0 } ? failure.StatusMessagesToSingleMessage() : response.ReasonPhrase ?? $"HTTP {(int)response.StatusCode}";
        BaseHttpException exception = new((int)response.StatusCode, message);
        if (failure?.ErrorCode is { Length: > 0 } errorCode)
        {
            exception.Data["errorCode"] = errorCode;
        }

        throw exception;
    }

    // a body that is not JSON (a proxy's page, an empty answer) is not an answer of a service: the caller gets the status and the reason
    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(s_json, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return default;
        }
        catch (NotSupportedException)
        {
            return default;
        }
    }
}
