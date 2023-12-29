using System;
using System.Linq;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class EventNameAttribute : Attribute, IEventNameProvider
{
    public EventNameAttribute([NotNull] string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name));
    }

    public virtual string Name { get; }

    public string GetName(Type eventType)
    {
        return Name;
    }

    public static string GetNameOrDefault<TEvent>()
    {
        return GetNameOrDefault(typeof(TEvent));
    }

    public static string GetNameOrDefault([NotNull] Type eventType)
    {
        Check.NotNull(eventType, nameof(eventType));

        return eventType
                   .GetCustomAttributes(true)
                   .OfType<IEventNameProvider>()
                   .FirstOrDefault()
                   ?.GetName(eventType)
               ?? eventType.FullName;
    }
}