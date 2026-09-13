using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace HealthBr.IntegrationTests.Api;

public sealed record LogEntry(string Category, LogLevel Level, string Message);

/// <summary>
/// Captures application logs in memory so integration tests can verify the
/// security-event logging required by spec 15.6.
/// </summary>
public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries.ToList();

    public void Clear() => _entries.Clear();

    public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class TestLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        // Scopes are not captured: assertions work on formatted messages.
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception)));
    }
}
