using System;

namespace HsnSoft.Base.EventBus;

/// <summary>How an event type becomes the name it has on the broker.</summary>
public static class EventNames
{
    /// <summary>The event name of a type: its name without the configured prefix and suffix.</summary>
    public static string For(EventBusOptions options, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return Trim(options, eventType.Name);
    }

    /// <summary>Removes the configured prefix and suffix from a type name.</summary>
    public static string Trim(EventBusOptions options, string typeName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(typeName);

        string name = typeName;
        if (options.EventNamePrefix.Length > 0 && name.StartsWith(options.EventNamePrefix, StringComparison.Ordinal))
        {
            name = name[options.EventNamePrefix.Length..];
        }

        if (options.EventNameSuffix.Length > 0 && name.EndsWith(options.EventNameSuffix, StringComparison.Ordinal))
        {
            name = name[..^options.EventNameSuffix.Length];
        }

        return name;
    }
}
