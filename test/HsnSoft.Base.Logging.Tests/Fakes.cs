using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception, object? State, IReadOnlyList<IReadOnlyDictionary<string, object>> Scopes);

/// <summary>Remembers what was logged, under which category, and inside which scopes.</summary>
public sealed class ListLoggerFactory : ILoggerFactory
{
    public List<LogEntry> Entries { get; } = [];

    public List<string> Categories { get; } = [];

    public ILogger CreateLogger(string categoryName)
    {
        Categories.Add(categoryName);
        return new ListLogger(categoryName, Entries);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }

    private sealed class ListLogger(string category, List<LogEntry> entries) : ILogger
    {
        private readonly List<IReadOnlyDictionary<string, object>> _scopes = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IReadOnlyDictionary<string, object> values)
            {
                _scopes.Add(values);
            }

            return new Pop(_scopes);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Add(new LogEntry(category, logLevel, formatter(state, exception), exception, state, [.. _scopes]));

        private sealed class Pop(List<IReadOnlyDictionary<string, object>> scopes) : IDisposable
        {
            public void Dispose()
            {
                if (scopes.Count > 0)
                {
                    scopes.RemoveAt(scopes.Count - 1);
                }
            }
        }
    }
}
