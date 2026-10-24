using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Core.Tests;

public class LoggerFacilityExtensionsTests
{
    private sealed class ScopeLogger : ILogger
    {
        public object? Scope { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            Scope = state;
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }

    [Fact]
    public void BeginFacility_OpensAScopeThatNamesTheFacility()
    {
        ScopeLogger logger = new();

        logger.BeginFacility("ORDER_IMPORTED");

        Assert.Equal("ORDER_IMPORTED", Assert.IsAssignableFrom<IDictionary<string, object>>(logger.Scope)[LogFields.Facility]);
    }

    [Fact]
    public void BeginFacility_NeedsALoggerAndAName()
    {
        Assert.Throws<ArgumentNullException>(() => LoggerFacilityExtensions.BeginFacility(null!, "X"));
        Assert.Throws<ArgumentException>(() => new ScopeLogger().BeginFacility(" "));
    }
}
