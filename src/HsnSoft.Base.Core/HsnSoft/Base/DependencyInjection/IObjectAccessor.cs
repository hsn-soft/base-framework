using JetBrains.Annotations;

namespace HsnSoft.Base.DependencyInjection;

public interface IObjectAccessor<out T>
{
    [CanBeNull]
    T Value { get; }
}