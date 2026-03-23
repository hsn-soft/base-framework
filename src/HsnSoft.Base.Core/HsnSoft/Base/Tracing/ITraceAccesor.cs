using JetBrains.Annotations;

namespace HsnSoft.Base.Tracing;

public interface ITraceAccesor
{
    [CanBeNull]
    string GetCorrelationId();

    [CanBeNull]
    string GetUserId();

    [CanBeNull]
    string[] GetUserRoles();

    [CanBeNull]
    string GetClientLat();

    [CanBeNull]
    string GetClientLong();
    [CanBeNull]
    string GetClientChannel();

    [CanBeNull]
    string GetClientVersion();
}