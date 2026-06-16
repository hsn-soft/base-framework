namespace Hhs.Shared.Retry;

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
}