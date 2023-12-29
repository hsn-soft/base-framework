namespace HsnSoft.Base.Tracing;

public interface ITraceAccesor
{
    string? GetCorrelationId();

    string? GetUserId();

    string[] GetRoles();
}