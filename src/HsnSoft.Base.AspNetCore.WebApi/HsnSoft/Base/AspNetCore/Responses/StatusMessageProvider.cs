namespace HsnSoft.Base.AspNetCore.Responses;

/// <summary>
/// The text for a status code. Register your own (localized) one to replace the English default. A failure of the service (5xx) is a key, not a sentence:
/// the client shows its own text for it and the trace id is what to tell support.
/// </summary>
public interface IStatusMessageProvider
{
    string GetMessage(int statusCode);
}

public sealed class StatusMessageProvider : IStatusMessageProvider
{
    public string GetMessage(int statusCode) => statusCode switch
    {
        200 => "Success",
        201 => "Created",
        202 => "Accepted",
        204 => "No content",

        400 => "Bad request.",
        401 => "Unauthorized.",
        403 => "Forbidden.",
        404 => "Resource not found.",
        405 => "Method not allowed.",
        408 => "Request timeout.",
        415 => "Unsupported media type.",
        429 => "Too many requests.",
        500 => "INTERNAL_SERVER_ERROR",
        502 => "BAD_GATEWAY",
        503 => "SERVICE_UNAVAILABLE",
        504 => "GATEWAY_TIMEOUT",
        _ => "Request failed.",
    };
}
