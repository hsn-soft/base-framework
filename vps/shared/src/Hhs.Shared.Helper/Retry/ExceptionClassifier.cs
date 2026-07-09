using System.Net;

namespace Hhs.Shared.Helper.Retry;

public static class ExceptionClassifier
{
    public static bool IsRetryable(Exception ex)
    {
        if (ex is ProcessException processException)
            return processException.ErrorType == ProcessErrorType.Retryable;

        return ex is TimeoutException
            or TaskCanceledException
            or HttpRequestException
            or IOException;
    }

    /// <summary>
    /// Default HTTP-status classification for providers deciding their own response DTO's
    /// IsRetryable field: 5xx (server-side) and 429 (rate limit) are transient; every other 4xx
    /// (400/401/403/404/422, ...) is treated as a permanent/config error.
    /// </summary>
    public static bool IsRetryable(HttpStatusCode statusCode)
    {
        int code = (int)statusCode;
        return code >= 500 || code == (int)HttpStatusCode.TooManyRequests;
    }
}