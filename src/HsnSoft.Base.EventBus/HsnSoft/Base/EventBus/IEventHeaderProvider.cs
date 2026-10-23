using System.Collections.Generic;

namespace HsnSoft.Base.EventBus;

/// <summary>
/// Lets a product add what it knows about the caller to every message it publishes (the user, the client, the request's correlation id).
/// The event bus is a singleton, so implement this as a singleton too and read the current request through an accessor.
/// Values of the message that caused the new one take precedence over what is provided here.
/// </summary>
public interface IEventHeaderProvider
{
    /// <summary>The correlation id of the current request, or <see langword="null"/> when there is none.</summary>
    string? GetCorrelationId();

    /// <summary>Adds headers (user, roles, client details, ...) under the names the product keeps on the wire.</summary>
    void AddHeaders(IDictionary<string, string> headers);
}
