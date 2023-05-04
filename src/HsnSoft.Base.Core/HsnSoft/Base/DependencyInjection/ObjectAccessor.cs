using JetBrains.Annotations;

namespace HsnSoft.Base.DependencyInjection;

public class ObjectAccessor<T> : IObjectAccessor<T>
{
    public ObjectAccessor()
    {
    }

    public ObjectAccessor([CanBeNull] T obj)
    {
        Value = obj;
    }

    public T Value { get; set; }
}