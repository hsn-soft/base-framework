using JetBrains.Annotations;

namespace HsnSoft.Base.Tracing;

public interface ICorrelationIdProvider
{
    [NotNull]
    string Get();
}