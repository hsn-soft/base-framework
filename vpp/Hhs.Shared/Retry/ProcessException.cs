namespace Hhs.Shared.Retry;

public enum ProcessErrorType
{
    Retryable,
    NonRetryable
}

public sealed class ProcessException : Exception
{
    public ProcessErrorType ErrorType { get; }

    public ProcessException(
        string message,
        ProcessErrorType errorType,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorType = errorType;
    }
}