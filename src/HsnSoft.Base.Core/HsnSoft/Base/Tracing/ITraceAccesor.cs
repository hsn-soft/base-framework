using JetBrains.Annotations;

namespace HsnSoft.Base.Tracing;

public interface ITraceAccesor
{
    [CanBeNull]
    string GetCorrelationId();

    [CanBeNull]
    string GetUserId();

    [CanBeNull]
    string[] GetRoles();

    [CanBeNull]
    string GetChannel();
}