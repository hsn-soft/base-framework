using System;
using System.Collections.Generic;
using System.Linq;

namespace HsnSoft.Base.Collections;

/// <summary>Small helpers for sequences, collections and dictionaries.</summary>
public static class BaseCollectionExtensions
{
    /// <summary>The items as one text, with <paramref name="separator"/> between them (<see langword="null"/> items are empty).</summary>
    public static string JoinAsString<T>(this IEnumerable<T> source, string separator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(separator);

        return string.Join(separator, source);
    }

    /// <summary>The items that meet <paramref name="predicate"/> when <paramref name="condition"/> holds, all of them when it does not: a filter that is only there when its value was given.</summary>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        return condition ? source.Where(predicate) : source;
    }

    /// <summary>Whether the collection is <see langword="null"/> or has no item.</summary>
    public static bool IsNullOrEmpty<T>(this ICollection<T>? source) => source is null || source.Count == 0;

    /// <summary>The value of <paramref name="key"/>; when there is none the factory makes one, it is added and returned.</summary>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, Func<TKey, TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(factory);

        if (source.TryGetValue(key, out TValue? existing))
        {
            return existing;
        }

        TValue made = factory(key);
        source[key] = made;
        return made;
    }

    /// <summary>The first item that meets <paramref name="predicate"/>; when there is none the factory makes one, it is added and returned.</summary>
    public static T GetOrAdd<T>(this ICollection<T> source, Func<T, bool> predicate, Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(factory);

        foreach (T item in source)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        T made = factory();
        source.Add(made);
        return made;
    }
}
