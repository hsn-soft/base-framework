namespace HsnSoft.Base.AspNetCore.Responses;

public sealed class FrameworkStatusMessageProvider : IFrameworkStatusMessageProvider
{
    public string GetMessage(int statusCode) => statusCode switch
    {
        400 => "Bad request.",
        401 => "Unauthorized.",
        403 => "Forbidden.",
        404 => "Resource not found.",
        405 => "Method not allowed.",
        408 => "Request timeout.",
        415 => "Unsupported media type.",
        429 => "Too many requests.",
        500 => "Internal server error.",
        502 => "Bad gateway.",
        503 => "Service unavailable.",
        504 => "Gateway timeout.",
        _ => "Request failed."
    };
}