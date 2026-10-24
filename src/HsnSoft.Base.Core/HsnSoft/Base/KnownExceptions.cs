using System;

namespace HsnSoft.Base;

/// <summary>
/// Tells the errors the application expects from the ones it does not. A known error is the answer to something the caller did (a rule
/// that says no, a missing record, an argument that is wrong): hosting layers answer it with a client status (HTTP 4xx) and log it as a
/// Warning, and a message that fails with one is closed without a retry. Everything else (a null reference, a broken connection) is a
/// failure of the service: HTTP 500, logged as an Error, and a message that fails with it is retried and then reported.
/// An application adds its own known errors by deriving them from <see cref="BusinessException"/> or <see cref="DomainException"/>.
/// </summary>
public static class KnownExceptions
{
    public static bool IsKnown(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            BaseHttpException http => http.HttpStatusCode < 500,
            NotFoundException or BusinessRuleException or BusinessException or DomainException or ArgumentException or UnauthorizedAccessException => true,
            _ => false,
        };
    }
}
